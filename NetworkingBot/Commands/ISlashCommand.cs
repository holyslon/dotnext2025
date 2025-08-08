using Telegram.Bot.Types;

namespace NetworkingBot.Commands;

public interface ISlashCommand
{
    public string Action { get; }
    public string SlashCommand => $"/{Action}";
    
    public bool InMessage(Message message) => message.Text != null && message.Text.Equals(SlashCommand, StringComparison.Ordinal);
}

public static class ISlashCommandExtensions
{
    public static string SlashCommand(this ISlashCommand command) => command.SlashCommand;
}


