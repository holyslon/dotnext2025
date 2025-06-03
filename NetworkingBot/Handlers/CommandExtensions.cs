namespace NetworkingBot;

internal static class CommandExtensions
{
    public static bool IsEqual(this Command command, string? commandText)
    {
        return command.ToCommand().Equals(commandText, StringComparison.Ordinal);
    }

    public static string ToCommand(this Command command)
    {
        return command switch
        {
            Command.Start => "/start",
            Command.Join => "/join",
            Command.Cancel => "/cancel",
            Command.Matchmake => "/matchmake",
            Command.ReadyForAnother => "/ready_for_coffee",
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
    }
}