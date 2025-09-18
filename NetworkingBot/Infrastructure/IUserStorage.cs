using Telegram.Bot.Types;

namespace NetworkingBot.Infrastructure;

internal interface IUserStorage
{
    public class UserNotFound(long id) : Exception($"User {id} not found");

    ValueTask<bool> WithCreateOrGetUser(Chat chat, User user, Func<Domain.IUser, ValueTask<bool>> action,
        CancellationToken cancellationToken = default);

    ValueTask<bool> WithGetUser(long chatId, Func<Domain.IUser, ValueTask<bool>> action,
        CancellationToken cancellationToken = default);

    ValueTask<bool> Save(Domain.IUser domainPoll, CancellationToken cancellationToken = default);
}