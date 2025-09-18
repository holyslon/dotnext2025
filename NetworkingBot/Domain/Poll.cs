using System.Collections.Immutable;

namespace NetworkingBot.Domain;



internal class Poll(IUser domainUser, string pollId, ImmutableArray<ConversationTopic> topics)
{
    public IUser DomainUser { get; } = domainUser;
    public string PollId { get; } = pollId;
    public ImmutableArray<ConversationTopic> Topics { get; } = topics;
}