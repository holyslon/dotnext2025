using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace NetworkingBot.Handlers;

internal class JustWatchingCommandHandler(ILogger<JustWatching> logger, IUserStorage userStorage)
    : UserUniversalCommandHandler<JustWatching>(logger, userStorage)
{
    protected override async ValueTask<bool> Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
        Domain.IUser domainUser, long chatId)
    {
        if (domainUser.TryJustWatch())
        {
            await botClient.SendMessage(chatId,
                Texts.WaitingForYouToReturn(Commands.Commands.Join),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return true;
        }
        return false;
    }
}