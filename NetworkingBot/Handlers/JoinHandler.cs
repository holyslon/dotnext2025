using NetworkingBot.Conversations;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NetworkingBot.Handlers;

internal class JoinHandler(ConversationDb db, ILogger<JoinHandler> logger) : ITelegramEventHandler<Message>
{
    public Task OnEvent(ITelegramBotClient bot, Message eventPayload, CancellationToken cancellationToken)
    {
        return eventPayload.OnCommand(Command.Join, () =>
            db.When(eventPayload, UserState.JustObserving, async (c) =>
            {
                await bot.SendMessage(eventPayload.Chat.Id,
                    $"Cool we will find you someone to have coffee with! When you want cancel this activity just type '{Command.Cancel.ToCommand()}' to this bot.", cancellationToken: cancellationToken);
                await c.MoveToWhantsCoffe(cancellationToken);
            }));
    }
}