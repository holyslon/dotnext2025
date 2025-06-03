using NetworkingBot.Conversations;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class StartHandler(ConversationDb db, ILogger<StartHandler> logger)
    : ITelegramEventHandler<Message>, ITelegramEventHandler<CallbackQuery>
{
    public async Task OnEvent(ITelegramBotClient bot, Message eventPayload, CancellationToken cancellationToken)
    {
        var chatId = eventPayload.Chat.Id;
        await eventPayload.OnCommand(Command.Start, () => db.When(eventPayload, UserState.Unknown, async (conv) =>
        {
            await bot.SendMessage(chatId,
                "Hello there! If you want coffee with someone, just select yes. If you observing just select 'maybe later'",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("yes", $"{chatId}:yes"),
                    InlineKeyboardButton.WithCallbackData("maybe later", $"{chatId}:postpone")), cancellationToken: cancellationToken);
            conv.MoveToMeet();
        }));
    }

    public Task OnEvent(ITelegramBotClient bot, CallbackQuery eventPayload, CancellationToken cancellationToken)
    {
        return eventPayload.OnCallback(logger, handler =>
            handler.OnData("postpone", chatId => db.When(chatId, UserState.Meet,
                    async (conv) =>
                    {
                        await bot.SendMessage(chatId,
                            $"Ok. Nice to meet you. If you want join to random coffee later, just type '{Command.Join.ToCommand()}' to this bot.", cancellationToken: cancellationToken);
                        await conv.MoveToJustObserving(cancellationToken);
                    }))
                .OnData("yes", chatId => db.When(chatId, UserState.Meet,
                    async (conv) =>
                    {
                        await bot.SendMessage(chatId,
                            $"Cool we will find you someone to have coffee with! When you want cancel this activity just type '{Command.Cancel.ToCommand()}' to this bot.", cancellationToken: cancellationToken);
                        await conv.MoveToWhantsCoffe(cancellationToken);
                    })));
    }
}