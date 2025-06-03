namespace NetworkingBot.Conversations;

internal class Conversation(long chatId, long userId, string userName, IMatchmakingService matchmakingService)
{
    public UserState State { get; private set; } = UserState.Unknown;
    public long ChatId { get; } = chatId;
    public long UserId { get; } = userId;
    public string UserName { get; } = userName;

    public void MoveToMeet()
    {
        if (State == UserState.Unknown) State = UserState.Meet;
    }

    public async Task MoveToJustObserving(CancellationToken token)
    {
        if (State == UserState.Meet)
        {
            State = UserState.JustObserving;
            await matchmakingService.RemoveFromPool(this);
        }
    }

    public async Task MoveToWhantsCoffe(CancellationToken token)
    {
        if (State == UserState.Meet)
        {
            State = UserState.WhantsCoffe;
            await matchmakingService.AddToPool(this);
        }
    }
}