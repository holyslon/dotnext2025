using NetworkingBot.Conversations;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot;

public interface IMatchmakingService
{
    Task CreateEventFor(long userId, ITelegramBotClient botClient, CancellationToken cancellationToken);
    internal Task AddToPool(Conversation conversation);
    internal Task RemoveFromPool(Conversation conversation);
    Task TryReturnToPool(long chatId, ITelegramBotClient botClient, CancellationToken cancellationToken);
}

internal class MatchmakingService(ILogger<MatchmakingService> logger) : IMatchmakingService
{
    private record Event(Conversation One, Conversation Another);

    private readonly List<Conversation> _avaliableConversations = [];
    private readonly Dictionary<long, Event> _events = new();

    internal const string ReadyButtonText = "ready for another coffee";

    private static string MatchHappensMessage(long userId, string userName)
    {
        return
            $"Hey we find you a pair for coffee, just dm [{userName}](tg://user?id={userId}). When you ready for another round just press '{ReadyButtonText}' or type '{Command.ReadyForAnother.ToCommand()}'.";
    }

    public async Task CreateEventFor(long userId, ITelegramBotClient bot, CancellationToken cancellationToken)
    {
        var one = _avaliableConversations.FirstOrDefault(c => c.UserId == userId);
        var other = _avaliableConversations.FirstOrDefault(c => c.UserId != userId);
        if (one != null && other != null)
        {
            _avaliableConversations.Remove(one);
            _avaliableConversations.Remove(other);
            var e = new Event(one, other);
            _events.Add(e.One.ChatId, e);
            _events.Add(e.Another.ChatId, e);
            await bot.SendMessage(one.ChatId,
                MatchHappensMessage(other.UserId, other.UserName),
                ParseMode.MarkdownV2,
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("ready for another coffee", $"{one.ChatId}:another")),
                cancellationToken: cancellationToken);
            await bot.SendMessage(other.ChatId,
                MatchHappensMessage(one.UserId, one.UserName),
                ParseMode.MarkdownV2,
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("ready for another coffee", $"{other.ChatId}:another")),
                cancellationToken: cancellationToken);
        }
    }

    public async Task AddToPool(Conversation conversation)
    {
        _avaliableConversations.Add(conversation);
    }

    public async Task RemoveFromPool(Conversation conversation)
    {
        var one = _avaliableConversations.Index().FirstOrDefault(c => c.Item.UserId == conversation.UserId);
        if (one.Item != null) _avaliableConversations.RemoveAt(one.Index);
    }

    private async Task TryReturnToPool(Conversation conversation, ITelegramBotClient botClient,
        CancellationToken cancellationToken)
    {
        if (conversation.State == UserState.WhantsCoffe)
        {
            await botClient.SendMessage(conversation.ChatId,
                "We are looking next person to meet. As soon as we find one - we will come back to you", cancellationToken: cancellationToken);
            _avaliableConversations.Add(conversation);
        }
    }
    
    public async Task TryReturnToPool(long chatId, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (_events.TryGetValue(chatId, out var e))
        {
            _events.Remove(e.One.ChatId);
            _events.Remove(e.Another.ChatId);
            await TryReturnToPool(e.One, botClient, cancellationToken);
            await TryReturnToPool(e.Another, botClient, cancellationToken);
        }
    }
}