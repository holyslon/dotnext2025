using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NetworkingBot.Handlers;

internal abstract class UserUniversalCommandHandler<TCommand>(ILogger<TCommand> logger, IUserStorage userStorage)
    : ISlashCommandHandler<TCommand>, IInlineCommandHandler<TCommand>
    where TCommand : ISlashCommand, IInlineCommand, new()

{
    public async ValueTask<bool> HandleAsync(ITelegramBotClient botClient, Chat chat, User? user, TCommand command,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope(new { chat.Id, command });
        if (user == null)
        {
            logger.LogError($"User is null");
            return false;
        }

        return await userStorage.WithCreateOrGetUser(chat, user, domainUser =>
            Handle(botClient, cancellationToken, domainUser, chat.Id), cancellationToken);
    }

    public async ValueTask<bool> HandleAsync(ITelegramBotClient botClient, long chatId, TCommand command,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope(new { chatId });
        return await userStorage.WithGetUser(chatId, domainUser => Handle(botClient, cancellationToken, domainUser, chatId),
            cancellationToken);
    }

    protected abstract ValueTask<bool> Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
        Domain.User domainUser, long chatId);
}

internal interface IMeetingStorage
{
    public class MeetingForUserNotFound(long id) : Exception($"Current meeting for user {id} not found");
    public class MeetingFoChatNotFound(long id) : Exception($"Current meeting for user with chat id {id} not found");
    ValueTask<bool> WithMeetingForUser(Chat chat, User user, Func<Domain.Meeting, ValueTask<bool>> action, CancellationToken cancellationToken);
    ValueTask<bool> WithMeetingForChat(long chatId, Func<Domain.Meeting, ValueTask<bool>> action, CancellationToken cancellationToken);
} 

internal abstract class MeetingUniversalCommandHandler<TCommand>(ILogger<TCommand> logger, IMeetingStorage meetingStorage)
    : ISlashCommandHandler<TCommand>, IInlineCommandHandler<TCommand>
    where TCommand : ISlashCommand, IInlineCommand, new()

{
    public async ValueTask<bool> HandleAsync(ITelegramBotClient botClient, Chat chat, User? user, TCommand command,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope(new { chat.Id, command });
        if (user == null)
        {
            logger.LogError($"User is null");
            return false;
        }

        return await meetingStorage.WithMeetingForUser(chat, user, meeting =>
            Handle(botClient, cancellationToken, meeting, chat.Id), cancellationToken);
    }

    public async ValueTask<bool> HandleAsync(ITelegramBotClient botClient, long chatId, TCommand command,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope(new { chatId });
        return await meetingStorage.WithMeetingForChat(chatId, meeting =>
            Handle(botClient, cancellationToken, meeting, chatId), cancellationToken);
    }

    protected abstract ValueTask<bool> Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
        Domain.Meeting meeting, long sourceChatId);
}