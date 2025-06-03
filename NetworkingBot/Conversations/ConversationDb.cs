namespace NetworkingBot.Conversations;

internal class ConversationDb(IMatchmakingService matchmakingService)
{
    private readonly Dictionary<long, Conversation> _conversations = new();

    public IEnumerable<Conversation> Conversations => _conversations.Values;

    public Conversation GetByChatId(long chatId)
    {
        if (_conversations.TryGetValue(chatId, out var conversation)) return conversation;
        throw new Exception("Unknown conversation");
    }

    public Conversation GetByChatUserId(long userId)
    {
        return _conversations.Values.First((conversation) => conversation.UserId == userId);
    }


    public Conversation GetByChatId(long chatId, long userId, string userName)
    {
        if (_conversations.TryGetValue(chatId, out var conversation))
        {
            return conversation;
        }
        else
        {
            conversation = new Conversation(chatId, userId, userName, matchmakingService);
            _conversations.Add(chatId, conversation);
            return conversation;
        }
    }
}