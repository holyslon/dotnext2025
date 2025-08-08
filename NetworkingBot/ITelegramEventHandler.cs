using Telegram.Bot;

namespace NetworkingBot;

internal interface ITelegramEventHandler<in T>
{
    ValueTask OnEvent(ITelegramBotClient bot, T eventPayload, CancellationToken cancellationToken);
}