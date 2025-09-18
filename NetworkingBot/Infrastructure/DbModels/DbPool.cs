using Microsoft.EntityFrameworkCore;

namespace NetworkingBot.Infrastructure.DbModels;

[Index(nameof(UserId))]
[Index(nameof(ExternalId))]
internal class DbPool
{
    public long Id { get; set; }
    public required long UserId { get; set; }
    public required string ExternalId { get; set; }
}