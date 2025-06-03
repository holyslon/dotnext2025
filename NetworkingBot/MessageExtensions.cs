using Telegram.Bot.Types;

namespace NetworkingBot;

internal static class MessageExtensions
{
    public static Task OnCommand(this Message message, Command command, Func<Task> action)
    {
        return command.IsEqual(message.Text) ? action() : Task.CompletedTask;
    }
}