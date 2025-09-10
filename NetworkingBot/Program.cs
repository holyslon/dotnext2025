using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetworkingBot;
using NetworkingBot.Infrastructure;
using Npgsql;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

var serviceName = "NetworkingBot";

builder.Logging.ClearProviders();

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

// Add services to the container.

var app = builder.Build();


app.MapGet("/user/{id}", async (string id, [FromServices] RedirectService redirectService, CancellationToken ct) => Results.Redirect(await redirectService.TgUrlById(id, ct)));
app.MapHealthChecks("/health", new HealthCheckOptions {Predicate = registration => registration.Tags.Contains("health")});
app.MapHealthChecks("/ready", new HealthCheckOptions {Predicate = registration => registration.Tags.Contains("ready")});

app.Run();