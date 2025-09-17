using Telegram.Bot;

namespace NetworkingBot;

internal interface ITelegramEventHandler<in T>
{
    ValueTask<bool> OnEvent(ITelegramBotClient bot, T eventPayload, CancellationToken cancellationToken);
}