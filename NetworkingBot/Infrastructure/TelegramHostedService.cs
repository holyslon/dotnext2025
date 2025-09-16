using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace NetworkingBot.Infrastructure;

public class TelegramHostedService(ILogger<TelegramHostedService> logger, 
    IUpdateHandler updateHandler, 
    IOptions<ServiceCollectionExtensions.AppOptions> appOptions, 
    TelegramBot bot)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(appOptions.Value.BaseUrl) && !appOptions.Value.BaseUrl.Contains("localhost"))
            {
                var url = appOptions.Value.BaseUrl+"/updates";
                var token = string.IsNullOrWhiteSpace(appOptions.Value.UpdateSecretToken) ? null : appOptions.Value.UpdateSecretToken;
                await bot.SetWebhook(url, secretToken: token, cancellationToken: stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            else
            {

                await bot.ReceiveAsync(updateHandler, cancellationToken: stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            logger.LogError(e, "Fail to receive update");
        }
    }
}