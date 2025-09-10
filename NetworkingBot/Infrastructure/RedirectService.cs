namespace NetworkingBot.Infrastructure;

public class RedirectService
{
    public ValueTask<string> TgUrlById(string id, CancellationToken ct)
    {
        return ValueTask.FromResult($"tg://user?id={id}");
    }
}