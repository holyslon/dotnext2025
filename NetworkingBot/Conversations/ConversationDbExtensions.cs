using Telegram.Bot.Types;

namespace NetworkingBot.Conversations;

internal static class ConversationDbExtensions
{
    public static Task When(this ConversationDb db, Message message, UserState userState,
        Func<Conversation, Task> action)
    {
        if (message.From == null) //this is not actual user send message, no need to forward it
            return Task.CompletedTask;
        var conv = db.GetByChatId(message.Chat.Id, message.From.Id, message.From.Username ?? message.From.FirstName);
        return conv.State == userState ? action(conv) : Task.CompletedTask;
    }

    public static Task When(this ConversationDb db, long chatId, UserState userState,
        Func<Conversation, Task> action)
    {
        var conv = db.GetByChatId(chatId);
        return conv.State == userState ? action(conv) : Task.CompletedTask;
    }
}