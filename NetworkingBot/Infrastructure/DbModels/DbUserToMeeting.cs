namespace NetworkingBot.Infrastructure.DbModels;

internal class DbUserToMeeting
{
    public long Id { get; set; }
    public required long UserId { get; set; }
    public required long MeetingId { get; set; }
    
    public required bool FeedbackAvailable { get; set; }
}