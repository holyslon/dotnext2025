using NetworkingBot.Domain;

namespace NetworkingBot.Infrastructure;

internal interface IMatchService
{
    ValueTask<(bool, IMeeting? meeting)> TryFindMatch(IUser.SearchInfo searchInfo, CancellationToken cancellationToken);
}