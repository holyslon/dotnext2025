using NetworkingBot.Domain;

namespace NetworkingBot.Infrastructure;

internal interface IMatchService
{
    ValueTask<(bool, Meeting? meeting)> TryFindMatch(User.SearchInfo searchInfo);
}

internal class MatchService(IUserStorage storage) : IMatchService
{
    
    private readonly Dictionary<long, User.SearchInfo> _searchData = new();
    private readonly List<Meeting> _meetings = [];
    
    public async ValueTask<(bool, Meeting? meeting)> TryFindMatch(User.SearchInfo searchInfo)
    {
        var one = await storage.GetUserAsync(searchInfo.UserId);
        if (!one.CanBeInMatchResult)
        {
            return (false, null);
        }
        _searchData[searchInfo.UserId] = searchInfo;
        while (true)
        {
            var other = _searchData.Values.FirstOrDefault(x => x.UserId != searchInfo.UserId);
            if (other == null) return (false, null);
            var another = await storage.GetUserAsync(other.UserId);
            if (!another.CanBeInMatchResult)
            {
                _searchData.Remove(other.UserId);
                continue;
            }
            var meeting = new Meeting(one, another);
            _meetings.Add(meeting);
            return (true, meeting);

        }
    }
}