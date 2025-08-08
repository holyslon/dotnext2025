using NetworkingBot.Domain;

namespace NetworkingBot.Infrastructure;

internal interface IPollStorage
{
    ValueTask Save(Poll poll, CancellationToken cancellationToken);
    ValueTask<Poll> GetById(string eventPayloadPollId, CancellationToken cancellationToken);
}

internal class PollStorage : IPollStorage
{
    private readonly Dictionary<string, Poll> polls = new();
    
    public ValueTask Save(Poll poll, CancellationToken cancellationToken)
    {
        polls[poll.PollId] = poll;
        return ValueTask.CompletedTask;
    }

    public ValueTask<Poll> GetById(string eventPayloadPollId, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(polls[eventPayloadPollId]);
    }
}