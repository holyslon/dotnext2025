using System.Collections.Immutable;
using Telegram.Bot.Types;

namespace NetworkingBot.Domain;

public class ConversationTopic(string id, string name)
{
    public string Name { get; } = name;
    public string Id { get; } = id;
}


public interface IUser
{
    public enum ParticipationMode
    {
        Unknown,
        Online,
        Offline
    }

    public bool TryOptIn();

    public bool TryOptOut();

    public bool TryOnlineParticipation();

    public bool TryOfflineParticipation();

    public bool TryReadyToParticipate();

    public record SearchInfo(
        long UserId,
        string Name,
        IReadOnlyCollection<ConversationTopic> Topics,
        ParticipationMode ParticipationMode)
    {
        public static SearchInfo Empty => new(0, "", ImmutableList<ConversationTopic>.Empty, ParticipationMode.Unknown);
    };

    public bool TryGetSearchInfo(out SearchInfo searchInfo);

    public void SetConversationTopics(ImmutableArray<ConversationTopic> topics);

    public record IdType(long ChatId, long UserId);

    public IdType Id { get; }

    public record LinkData(long UserId, string Name);

    public LinkData Link { get; }
    bool TryJustWatch();
}

public interface IMeeting
{
    public record User(IUser.LinkData LinkData, long ChatId, bool FeedbackAvailible);
    public User One { get; }
    public User Another { get; }

    public IEnumerable<User> OtherUsers { get; }
    public bool InProgress { get; }
    public User Source { get; }
    public bool IsCompleted { get; }

    public ValueTask<bool> TryCancel(CancellationToken cancellationToken);

    public ValueTask<bool> TryCompleted(CancellationToken cancellationToken);

    public ValueTask<bool> TrySubmitFeedback(string? eventPayloadText, CancellationToken cancellationToken);
}