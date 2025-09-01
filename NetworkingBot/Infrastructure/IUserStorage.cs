using Telegram.Bot.Types;

namespace NetworkingBot.Infrastructure;

internal interface IUserStorage
{
    public class UserNotFound(long id) : Exception($"User {id} not found");

    ValueTask WithCreateOrGetUser(Chat chat, User user, Func<Domain.User, ValueTask> action,
        CancellationToken cancellationToken = default);

    ValueTask WithGetUser(long chatId, Func<Domain.User, ValueTask> action,
        CancellationToken cancellationToken = default);

    ValueTask Save(Domain.User domainPoll, CancellationToken cancellationToken = default);
}