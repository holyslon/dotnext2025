using Telegram.Bot;

namespace NetworkingBot;

internal interface ITelegramEventHandler<T>
{
    Task OnEvent(ITelegramBotClient bot, T eventPayload, CancellationToken cancellationToken);
}