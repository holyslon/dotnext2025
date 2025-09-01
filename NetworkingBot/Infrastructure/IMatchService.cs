using NetworkingBot.Domain;

namespace NetworkingBot.Infrastructure;

internal interface IMatchService
{
    ValueTask<(bool, Meeting? meeting)> TryFindMatch(User.SearchInfo searchInfo, CancellationToken cancellationToken);
}