using Microsoft.EntityFrameworkCore;

namespace NetworkingBot.Infrastructure.DbModels;

[Index(nameof(UserId))]
[Index(nameof(MeetingId))]
internal class DbFeedback
{
    public long Id { get; set; }
    public required long UserId { get; set; }
    public required long MeetingId { get; set; }
    public required string Feedback { get; set; }
}