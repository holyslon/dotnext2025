using Microsoft.EntityFrameworkCore;

namespace NetworkingBot.Infrastructure.DbModels;

[Index(nameof(PoolId))]
internal class DbPoolToDbTopic
{
    public long Id { get; set; }
    public required long PoolId { get; set; }
    public required long TopicId { get; set; }
    public required int Index { get; set; }
}