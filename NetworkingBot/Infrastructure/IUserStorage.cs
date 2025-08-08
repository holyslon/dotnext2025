using Telegram.Bot.Types;

namespace NetworkingBot.Infrastructure;

internal interface IUserStorage
{
    public class UserNotFound(long id): Exception($"User {id} not found");
    
    ValueTask<Domain.User> GetUserAsync(long userId);
    
    ValueTask WithCreateOrGetUser(Chat chat, User? user, Func<Domain.User, ValueTask> action,  CancellationToken cancellationToken = default);
    ValueTask WithGetUser(long chatId, Func<Domain.User, ValueTask> action, CancellationToken cancellationToken = default);
    ValueTask Save(Domain.User domainPoll, CancellationToken cancellationToken = default);
}

internal class UserStorage : IUserStorage
{
    private readonly Dictionary<long, Domain.User> _storage = new();
    private readonly Dictionary<long, Domain.User> _byUserId = new();

    public ValueTask<Domain.User> GetUserAsync(long userId)
    {
        if (_byUserId.TryGetValue(userId, out var user))
        {
            return ValueTask.FromResult(user);
        }
        throw new IUserStorage.UserNotFound(userId);
    }

    public ValueTask WithCreateOrGetUser(Chat chat, User? user, Func<Domain.User, ValueTask> action, CancellationToken cancellationToken = default)
    {
        if (!_storage.TryGetValue(chat.Id, out var domainUser))
        {
            if (user != null)
            {
                domainUser = new Domain.User(chat.Id, user.Id, user.Username ?? user.FirstName);
                _storage.Add(chat.Id, domainUser);
                _byUserId.Add(user.Id, domainUser);
            }
        }

        return domainUser != null ? action(domainUser) : ValueTask.CompletedTask;
    }

    public ValueTask WithGetUser(long chatId, Func<Domain.User, ValueTask> action, CancellationToken cancellationToken = default)
    {
        return _storage.TryGetValue(chatId, out var domainUser) ? action(domainUser) : ValueTask.CompletedTask;
    }

    public ValueTask Save(Domain.User user, CancellationToken cancellationToken= default)
    {
        _storage[user.Id.ChatId] = user;
        _byUserId[user.Id.UserId] = user;
        return ValueTask.CompletedTask;
    }
}