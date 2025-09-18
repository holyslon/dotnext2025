using Microsoft.EntityFrameworkCore;

namespace NetworkingBot.Infrastructure.DbModels;

[Index(nameof(UserId))]
internal class DbUserToDbTopic
{
    public long Id { get; set; }
    public required long UserId { get; set; }
    public required long TopicId { get; set; }
}