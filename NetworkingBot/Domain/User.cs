using System.Collections.Immutable;

namespace NetworkingBot.Domain;

public class ConversationTopic(string name)
{
    public string Name { get; } = name;
}

public class User(long chatId, long userId, string name)
{
    public enum ParticipationMode
    {
        Unknown,
        Online,
        Offline
    }
    
    private ParticipationMode _participationMode = ParticipationMode.Unknown;
    private readonly List<ConversationTopic> _topics = [];
    
    private bool _isActive;
    private bool _readyToParticipate;
    private Meeting? _currentMeeting;
    
    public void OptIn()
    {
        _isActive = true;
    }

    public void OptOut()
    {
        _isActive = false;
        _participationMode = ParticipationMode.Unknown;
        _readyToParticipate = false;
    }

    public void OnlineParticipation()
    {
        if (_isActive)
        {
            _participationMode = ParticipationMode.Online;
        }
    }
    public void OfflineParticipation()
    {
        if (_isActive)
        {
            _participationMode = ParticipationMode.Offline;
        }
    }

    public IReadOnlyCollection<ConversationTopic> AddConversationTopic(ConversationTopic topic)
    {
        _topics.Add(topic);
        return [.._topics];
    }

    public void ReadyToParticipate()
    {
        _readyToParticipate = true;
    }

    public record SearchInfo(long UserId, string Name, IReadOnlyCollection<ConversationTopic> Topics, ParticipationMode ParticipationMode)
    {
        public static SearchInfo Empty => new(0, "", ImmutableList<ConversationTopic>.Empty, ParticipationMode.Unknown);
    };

    public bool TryGetSearchInfo(out SearchInfo searchInfo)
    {
        if (_readyToParticipate)
        {
            searchInfo = new SearchInfo(userId, name, [.._topics], _participationMode);
            return true;
        } 
        searchInfo = SearchInfo.Empty;
        return false;
    }

    public bool TryAddMeeting(Meeting meeting)
    {
        if (_isActive && _readyToParticipate && _currentMeeting == null)
        {
            _currentMeeting = meeting;
            return true;
        }
        return false;
    }

    public void MeetingCompleted()
    {
        _currentMeeting = null;
    }

    public void SetConversationTopics(ImmutableArray<ConversationTopic> topics)
    {
        _topics.Clear();
        _topics.AddRange(topics);
    }
    
    public record IdType(long ChatId, long UserId);
    
    public IdType Id => new(chatId, userId);

    public record LinkData(long UserId, string Name);

    public LinkData Link => new(userId, name);

    public void MeetingCanceled()
    {
        _currentMeeting = null;
    }
    
    public bool CanBeInMatchResult => _isActive &&  _readyToParticipate;
}

public class Meeting(User one, User another)
{
    public User One { get; } = one;
    public User Another { get; } = another;
    
};