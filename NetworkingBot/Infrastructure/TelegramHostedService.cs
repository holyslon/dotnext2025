using Telegram.Bot;
using Telegram.Bot.Polling;

namespace NetworkingBot;

public class TelegramHostedService(ILogger<TelegramHostedService> logger, IUpdateHandler updateHandler, TelegramBot bot)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await bot.ReceiveAsync(updateHandler, cancellationToken: stoppingToken);
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