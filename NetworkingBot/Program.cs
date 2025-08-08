using NetworkingBot;
using NetworkingBot.Infrastructure;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var profile = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

builder.Configuration
    .AddJsonFile("appsettings.json", true, false)
    .AddJsonFile("appsettings.local.json", true, false)
    .AddJsonFile($"appsettings.{profile}.json", true, false)
    .AddEnvironmentVariables("NETWORKINGBOT_")
    .AddCommandLine(args);

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


builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));

builder.Services.AddSingleton<TelegramBot>();
builder.Services.AddHostedService<TelegramHostedService>();

builder.Services.AddNetworkingBot();

// Add services to the container.

var app = builder.Build();


// Configure the HTTP request pipeline.

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
        .ToArray();
    return forecast;
});

app.Run();

namespace NetworkingBot
{
    internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}