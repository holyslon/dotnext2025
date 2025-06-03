using Telegram.Bot;
using Telegram.Bot.Types;

namespace NetworkingBot.Handlers;

internal class EventFinishedHandler(IMatchmakingService service, ILogger<EventFinishedHandler> logger)
    : ITelegramEventHandler<CallbackQuery>, ITelegramEventHandler<Message>
{
    public Task OnEvent(ITelegramBotClient bot, CallbackQuery eventPayload, CancellationToken cancellationToken)
    {
        return eventPayload.OnCallback(logger, (conf) =>
            conf.OnData("another", id=>service.TryReturnToPool(id, bot, cancellationToken)));
    }

    public async Task OnEvent(ITelegramBotClient bot, Message eventPayload, CancellationToken cancellationToken)
    {
        await eventPayload.OnCommand(Command.ReadyForAnother, () => service.TryReturnToPool(eventPayload.Chat.Id, bot, cancellationToken));
    }
}