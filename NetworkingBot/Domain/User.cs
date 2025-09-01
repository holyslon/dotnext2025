using System.Collections.Immutable;

namespace NetworkingBot.Domain;

public class ConversationTopic(string id, string name)
{
    public string Name { get; } = name;
    public string Id { get; } = id;
}

public interface IUserBackend
{
    bool IsActive { get; set; }
    User.ParticipationMode ParticipationMode { get; set; }
    bool ReadyToParticipate { get; set; }
    long UserId { get; }
    string Name { get; }
    IReadOnlyCollection<ConversationTopic> Topics { get; }
    long ChatId { get; }
    void UpdateTopics(ImmutableArray<ConversationTopic> topics);
    void CancelMeeting();
    void MeetingCompleted();
}

public class User(IUserBackend storage)
{
    public IUserBackend Storage => storage;
    public enum ParticipationMode
    {
        Unknown,
        Online,
        Offline
    }

    public void OptIn()
    {
        storage.IsActive = true;
    }

    public void OptOut()
    {
        storage.IsActive = false;
        storage.ParticipationMode = ParticipationMode.Unknown;
        storage.ReadyToParticipate = false;
    }

    public void OnlineParticipation()
    {
        if (storage.IsActive) storage.ParticipationMode = ParticipationMode.Online;
    }

    public void OfflineParticipation()
    {
        if (storage.IsActive) storage.ParticipationMode = ParticipationMode.Offline;
    }

    public void ReadyToParticipate()
    {
        storage.ReadyToParticipate = true;
    }

    public record SearchInfo(
        long UserId,
        string Name,
        IReadOnlyCollection<ConversationTopic> Topics,
        ParticipationMode ParticipationMode)
    {
        public static SearchInfo Empty => new(0, "", ImmutableList<ConversationTopic>.Empty, ParticipationMode.Unknown);
    };

    public bool TryGetSearchInfo(out SearchInfo searchInfo)
    {
        if (storage.ReadyToParticipate)
        {
            searchInfo = new SearchInfo(storage.UserId, storage.Name, storage.Topics, storage.ParticipationMode);
            return true;
        }

        searchInfo = SearchInfo.Empty;
        return false;
    }

    public void MeetingCompleted()
    {
        storage.MeetingCompleted();
    }

    public void SetConversationTopics(ImmutableArray<ConversationTopic> topics)
    {
        storage.UpdateTopics(topics);
    }

    public record IdType(long ChatId, long UserId);

    public IdType Id => new(storage.ChatId, storage.UserId);

    public record LinkData(long UserId, string Name);

    public LinkData Link => new(storage.UserId, storage.Name);

    public void MeetingCanceled()
    {
        storage.CancelMeeting();
    }

    public bool CanBeInMatchResult => storage is { IsActive: true, ReadyToParticipate: true };
}

public class Meeting(User one, User another)
{
    public User One { get; } = one;
    public User Another { get; } = another;
};