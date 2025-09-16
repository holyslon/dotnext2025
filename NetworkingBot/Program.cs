using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Amazon.S3;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NetworkingBot;
using NetworkingBot.Infrastructure;
using Npgsql;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

[assembly:InternalsVisibleTo("NetworkingBotTest")]

var builder = WebApplication.CreateBuilder(args);

var profile = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

builder.Configuration
    .AddJsonFile("appsettings.json", true, false)
    .AddJsonFile("appsettings.local.json", true, false)
    .AddJsonFile($"appsettings.{profile}.json", true, false)
    .AddEnvironmentVariables("NETWORKINGBOT_")
    .AddCommandLine(args);


var tracingExporter = builder.Configuration.GetValue("UseTracingExporter", defaultValue: "CONSOLE").ToUpperInvariant();
var metricsExporter = builder.Configuration.GetValue("UseMetricsExporter", defaultValue: "CONSOLE").ToUpperInvariant();
var logExporter = builder.Configuration.GetValue("UseLogExporter", defaultValue: "CONSOLE").ToUpperInvariant();
var histogramAggregation = builder.Configuration.GetValue("HistogramAggregation", defaultValue: "EXPLICIT").ToUpperInvariant();
var ydbConnectionString = builder.Configuration.GetConnectionString("YDB");
var pgConnectionString = builder.Configuration.GetConnectionString("PG");
var otlpConnectionString = builder.Configuration.GetConnectionString("Otlp");

builder.Services.Configure<ServiceCollectionExtensions.AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));
builder.Services.Configure<AmazonS3Config>(builder.Configuration.GetSection("AmazonS3Config"));
builder.Services.Configure<LeaderboardHostedService.Options>(builder.Configuration.GetSection("Leaderboard"));

var serviceName = "NetworkingBot";

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(opts =>
{
    opts.IncludeScopes = true;
    opts.UseUtcTimestamp = true;
    opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    opts.JsonWriterOptions = new JsonWriterOptions { Indented = false, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)};

});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion:typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown", serviceInstanceId:Environment.MachineName))
    .WithLogging(logging =>
    {
        switch (logExporter)
        {
            case "OTLP":
                logging.AddOtlpExporter(otlpOptions =>
                {
                    // Use IConfiguration directly for Otlp exporter endpoint option.
                    otlpOptions.Endpoint = new Uri(otlpConnectionString ?? "http://localhost:4317");
                });
                break;
            default:
                logging.AddConsoleExporter();
                break;
        }
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
            .AddHttpClientInstrumentation();
        switch (tracingExporter)
        {
            case "OTLP":
                tracing.AddOtlpExporter(otlpOptions =>
                {
                    // Use IConfiguration directly for Otlp exporter endpoint option.
                    otlpOptions.Endpoint = new Uri(otlpConnectionString ?? "http://localhost:4317");
                });
                break;

            default:
                tracing.AddConsoleExporter();
                break;
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation().
            AddHttpClientInstrumentation().
            AddNpgsqlInstrumentation();
        if (histogramAggregation == "EXPONENTIAL")
            metrics.AddView(instrument => instrument.GetType().GetGenericTypeDefinition() == typeof(Histogram<>)
                ? new Base2ExponentialBucketHistogramConfiguration()
                : null);
        switch (metricsExporter)
        {
            case "OTLP":
                metrics.AddOtlpExporter(otlpOptions =>
                {
                    // Use IConfiguration directly for Otlp exporter endpoint option.
                    otlpOptions.Endpoint = new Uri(otlpConnectionString ?? "http://localhost:4317");
                });
                break;
            default:
                metrics.AddConsoleExporter();
                break;
        }
    });

builder.Services.AddHealthChecks()
    .AddNpgSql(pgConnectionString!, 
        name: "PostgreSQL Database", 
        failureStatus: HealthStatus.Unhealthy, 
        tags: ["database", "postgres", "ready"])
    .AddCheck<TelegramHealthCheck>(name: "telegram", failureStatus: HealthStatus.Unhealthy, tags: ["external", "telegram", "ready"]);



builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));

builder.Services.AddSingleton<TelegramBot>();
builder.Services.AddHostedService<TelegramHostedService>();

builder.Services.AddNetworkingBot(pgConnectionString!);
// builder.Services.ConfigureTelegramBot<Microsoft.AspNetCore.Http.Json.JsonOptions>(opts => opts.SerializerOptions);

// Add services to the container.

var app = builder.Build();


app.MapGet("/user/{id}", async (string id, [FromServices] RedirectService redirectService, CancellationToken ct) => Results.Redirect(await redirectService.TgUrlById(id, ct)));
app.MapHealthChecks("/health", new HealthCheckOptions {Predicate = registration => registration.Tags.Contains("health")});
app.MapHealthChecks("/ready", new HealthCheckOptions {Predicate = registration => registration.Tags.Contains("ready")});
app.MapPost("/updates",
    async (
        [FromBody] Update update, 
        [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")]string? secretToken,
        [FromServices] IUpdateHandler updateHandler, 
        [FromServices] TelegramBot bot, 
        [FromServices] ILogger<UpdateHandler> logger,
        [FromServices] IOptionsSnapshot<ServiceCollectionExtensions.AppOptions> appOptions,
        CancellationToken ct) =>
    {
        using var _ =logger.BeginScope(new { update });
        var appToken = appOptions.Value.UpdateSecretToken;
        if (!string.IsNullOrEmpty(appToken) && !secretToken!.Equals(appToken, StringComparison.Ordinal))
        {
            using var __ =logger.BeginScope(new { secretToken });
            logger.LogInformation("Update secret token is different than secret token");
            return Results.Unauthorized();
        }
        try
        {
            await updateHandler.HandleUpdateAsync(bot, update, ct);
            return Results.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Fail to handle update");
            return Results.StatusCode(500);
        }
    });
app.Run();