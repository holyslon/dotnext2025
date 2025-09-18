using NetworkingBot.Domain;

namespace NetworkingBot.Infrastructure;

internal interface IPollStorage
{    
    public class PoolNotFound(string id) : Exception($"Pool {id} not found");
    ValueTask Save(IUser user, string poolId, IReadOnlyList<ConversationTopic> topics, CancellationToken cancellationToken);
    ValueTask<Poll> GetById(string eventPayloadPollId, CancellationToken cancellationToken);
}