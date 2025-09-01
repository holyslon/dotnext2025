using NetworkingBot.Domain;

namespace NetworkingBot.Infrastructure;

internal interface IConversationTopicStorage
{
    public ValueTask<IReadOnlyList<ConversationTopic>> GetTopics(CancellationToken cancellationToken = default);
}