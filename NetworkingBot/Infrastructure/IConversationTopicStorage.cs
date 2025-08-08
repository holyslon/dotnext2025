using NetworkingBot.Domain;

namespace NetworkingBot.Infrastructure;

internal interface IConversationTopicStorage
{
    public IEnumerable<ConversationTopic> Topics { get; }
}

internal class ConversationTopicStorage : IConversationTopicStorage
{
    public IEnumerable<ConversationTopic> Topics { get; } =
    [
        new("F#"),
        new("Async"),
        new("PostgreSql"),
        new("Managment"),
        new("Event Sourcing")
    ];
}