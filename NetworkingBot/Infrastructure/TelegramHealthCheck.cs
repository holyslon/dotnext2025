using Microsoft.Extensions.Diagnostics.HealthChecks;
using Telegram.Bot;

namespace NetworkingBot.Infrastructure;

public class TelegramHealthCheck(TelegramBot botClient, ILogger<TelegramHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        try
        {
            await botClient.GetMe(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Fail to health tg bot api");
            return HealthCheckResult.Unhealthy();
        }
    }
}