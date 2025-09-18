namespace NetworkingBot.Infrastructure;

public interface IApplicationClearer
{
    public ValueTask Clear(CancellationToken cancellationToken = default);
}