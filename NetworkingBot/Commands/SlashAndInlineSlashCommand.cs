namespace NetworkingBot.Commands;

public abstract class SlashAndInlineSlashCommand(string text, string action) : IInlineCommand, ISlashCommand
{
    public string Text { get; } = text;
    public string DataTag => Action;
    public string Action { get; } = action;
}