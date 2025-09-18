using NameGenerator.Generators;
using NetworkingBot.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NetworkBotTest;

internal static class Create
{
    private static readonly Random Random = new();
    private static readonly RealNameGenerator Generator = new();

    internal static User User(long id = Default.UserId, string username = Default.Username, bool newUser = false)
    {
        return new User
        {
            Id = newUser ? Random.NextInt64() : id,
            Username = newUser ? Generator.Generate() : username
        };
    }

    internal static Chat Chat(long id = Default.ChatId, bool newChat = false)
    {
        return new Chat
        {
            Id = newChat ? Random.NextInt64() : id
        };
    }

    internal static Message Message(string text, Chat? chat = null, User? user = null)
    {
        return new Message
        {
            Text = text,
            Chat = chat ?? Chat(),
            From = user ?? User()
        };
    }

    internal static CallbackQuery CallbackQuery(string data)
    {
        return new CallbackQuery
        {
            Data = data
        };
    }

    internal static CallbackQuery CallbackQuery(this IInlineCommand command, Chat? chat = null)
    {
        chat ??= Chat();
        var button = command.Button(chat);
        return CallbackQuery(button.CallbackData!);
    }

    internal static string ForChat(this string action, Chat? chat = null)
    {
        chat ??= Chat();
        return $"{chat.Id}:{action}";
    }

    internal static PollAnswer PollAnswer()
    {
        return new PollAnswer
        {
        };
    }

    internal static Update Update(Message? message = null, CallbackQuery? callback = null, Poll? poll = null,
        PollAnswer? pollAnswer = null)
    {
        var update = new Update();
        if (message != null) update.Message = message;

        if (callback != null) update.CallbackQuery = callback;

        if (pollAnswer != null) update.PollAnswer = pollAnswer;
        if (poll != null) update.Poll = poll;
        return update;
    }

    internal static Poll Poll(string id, params PollOption[] options)
    {
        return new Poll
        {
            Id = id,
            Options = options,
            Type = PollType.Regular
        };
    }

    internal static PollOption[] Vote(this PollOption[] options, string optionText, int votes = 1)
    {
        foreach (var option in options)
            if (optionText.Equals(option.Text, StringComparison.InvariantCulture))
                option.VoterCount += votes;

        return options;
    }


    internal static NetworkingBot.Domain.IUser.LinkData LinkData(this User user)
    {
        return new NetworkingBot.Domain.IUser.LinkData(user.Id, user.Username ?? user.FirstName);
    }

    internal static async ValueTask<(User, Chat)> OnlineUser(this UpdateHandlerAndBot updateHandler,
        params string[] interests)
    {
        var user = User(newUser: true);
        var chat = Chat(newChat: true);
        await updateHandler.Update(Message("/start", user: user, chat: chat));
        await updateHandler.Update(callback: Commands.Join.CallbackQuery(chat));
        await updateHandler.Update(callback: Commands.Online.CallbackQuery(chat));
        var options = updateHandler.Mock.Pool().LastPollOption;
        foreach (var interest in interests) options.Vote(interest);
        await updateHandler.Update(poll: Poll(updateHandler.Mock.Pool().LastPollId, options));
        return (user, chat);
    }
    
    internal static async ValueTask<(User, Chat)> OfflineUser(this UpdateHandlerAndBot updateHandler,
        params string[] interests)
    {
        var user = User(newUser: true);
        var chat = Chat(newChat: true);
        await updateHandler.Update(Message("/start", user: user, chat: chat));
        await updateHandler.Update(callback: Commands.Join.CallbackQuery(chat));
        await updateHandler.Update(callback: Commands.Offline.CallbackQuery(chat));
        var options = updateHandler.Mock.Pool().LastPollOption;
        foreach (var interest in interests) options.Vote(interest);
        await updateHandler.Update(poll: Poll(updateHandler.Mock.Pool().LastPollId, options));
        return (user, chat);
    }

    internal static async Task Update(this UpdateHandlerAndBot updateHandler, Message? message = null,
        CallbackQuery? callback = null, Poll? poll = null)
    {
        await updateHandler.HandleUpdateAsync(Update(message, callback, poll));
    }

    internal static async Task<(Chat Chat, User User)> RegisteredUser(this UpdateHandlerAndBot updateHandler)
    {
        var chat = Chat(newChat: true);
        var user = User(newUser: true);
        await updateHandler.Update(Message("/start", chat, user));
        await updateHandler.Update(callback: CallbackQuery("yes".ForChat(chat)));
        return (chat, user);
    }
}