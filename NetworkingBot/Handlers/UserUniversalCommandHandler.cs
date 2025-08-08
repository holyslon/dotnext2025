using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NetworkingBot.Handlers;

internal abstract class UserUniversalCommandHandler<TCommand>(ILogger<TCommand> logger, IUserStorage userStorage) : ISlashCommandHandler<TCommand>, IInlineCommandHandler<TCommand>
    where TCommand: ISlashCommand, IInlineCommand, new()

{
    public async ValueTask HandleAsync(ITelegramBotClient botClient, Chat chat, User? user, TCommand command,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope(new {chat.Id});
        await userStorage.WithCreateOrGetUser(chat, user,  domainUser =>
            Handle(botClient, cancellationToken, domainUser, chat.Id), cancellationToken);
    }
    public async ValueTask HandleAsync(ITelegramBotClient botClient, long chatId, TCommand command,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope(new {chatId});
        await userStorage.WithGetUser(chatId, domainUser => Handle(botClient, cancellationToken, domainUser, chatId), cancellationToken);
    }

    protected abstract ValueTask Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
        Domain.User domainUser, long chatId);


}