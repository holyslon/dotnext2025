using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class StartCommandHandler(ILogger<StartCommandHandler> logger, IUserStorage userStorage)
    : ISlashCommandHandler<StartCommand>
{
    public async ValueTask<bool> HandleAsync(ITelegramBotClient botClient, Chat chat, User? user, StartCommand command,
        CancellationToken cancellationToken = default)
    {
        using var _ = logger.BeginScope(new { chat.Id });
        if (user == null)
        {
            logger.LogError($"User is null");
            return false;
        }

       return await userStorage.WithCreateOrGetUser(chat, user, async _ =>
        {
            await botClient.SendMessage(chat.Id, Texts.Welcome(Commands.Commands.Join, Commands.Commands.JustWatching),
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(Commands.Commands.Join.Button(chat.Id),
                    Commands.Commands.JustWatching.Button(chat.Id)),
                cancellationToken: cancellationToken);
            return true;
        }, cancellationToken);
    }
}