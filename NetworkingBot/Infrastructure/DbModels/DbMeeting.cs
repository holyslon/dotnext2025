using System.ComponentModel.DataAnnotations.Schema;

namespace NetworkingBot.Infrastructure.DbModels;

internal class DbMeeting
{
    public long Id { get; set; }
    public required int Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    [NotMapped]
    public StatusEnum TypedStatus
    {
        get => Status switch
        {
            1 => StatusEnum.Current,
            2 => StatusEnum.Cancelled,
            3 => StatusEnum.Finished,
            _ => StatusEnum.Unknown,
        };
        set
        {
            Status = value switch
            {
                StatusEnum.Unknown => 0,
                StatusEnum.Current => 1,
                StatusEnum.Cancelled => 2,
                StatusEnum.Finished => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
    }

    internal enum StatusEnum
    {
        Unknown,
        Current,
        Cancelled,
        Finished
    }
}