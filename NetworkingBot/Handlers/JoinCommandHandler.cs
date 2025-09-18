using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class JoinCommandHandler(ILogger<JoinCommand> logger, IUserStorage userStorage)
    : UserUniversalCommandHandler<JoinCommand>(logger, userStorage)
{
    protected override async ValueTask<bool> Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
        Domain.IUser domainUser, long chatId)
    {
        if (domainUser.TryOptIn())
        {
            await botClient.SendMessage(chatId,
                Texts.ChooseOnlineOrOffline(Commands.Commands.Online, Commands.Commands.Offline),
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(Commands.Commands.Online.Button(chatId),
                    Commands.Commands.Offline.Button(chatId)),
                cancellationToken: cancellationToken);
            return true;
        }
        return false;
    }
}