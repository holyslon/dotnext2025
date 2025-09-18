namespace NetworkingBot.Infrastructure.DbModels;

internal class DbUser
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public required long TgUserId { get; set; }
    public required long ChatId { get; set; }
    public required int State { get; set; }
    public required int ParticipationMode { get; set; }
}