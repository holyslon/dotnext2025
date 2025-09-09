using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetworkingBot;
using NetworkingBot.Infrastructure;
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


var ydbConnectionString = builder.Configuration.GetConnectionString("YDB");
var pgConnectionString = builder.Configuration.GetConnectionString("PG");

builder.Services.Configure<ServiceCollectionExtensions.AppOptions>(builder.Configuration.GetSection("App"));

var serviceName = "NetworkingBot";
builder.Logging.AddOpenTelemetry((options) =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
        .AddConsoleExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddConsoleExporter())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddConsoleExporter());

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