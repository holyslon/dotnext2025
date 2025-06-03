using NetworkingBot.Conversations;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NetworkingBot.Handlers;

internal class CancelHandler(ConversationDb db, ILogger<CancelHandler> logger) : ITelegramEventHandler<Message>
{
    public Task OnEvent(ITelegramBotClient bot, Message eventPayload, CancellationToken cancellationToken)
    {
        return eventPayload.OnCommand(Command.Cancel, () =>
            db.When(eventPayload, UserState.WhantsCoffe, async (c) =>
            {
                await bot.SendMessage(eventPayload.Chat.Id,
                    $"Ok. Nice to meet you. If you want join to random coffee later, just type '{Command.Join.ToCommand()}' to this bot.", cancellationToken: cancellationToken);
                await c.MoveToJustObserving(cancellationToken);
            }));
    }
}