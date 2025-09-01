using System.Collections.Immutable;

namespace NetworkingBot.Domain;



internal class Poll(User domainUser, string pollId, ImmutableArray<ConversationTopic> topics)
{
    public User DomainUser { get; } = domainUser;
    public string PollId { get; } = pollId;
    public ImmutableArray<ConversationTopic> Topics { get; } = topics;
}