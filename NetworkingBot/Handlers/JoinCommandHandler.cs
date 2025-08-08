using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;



internal class JoinCommandHandler(ILogger<JoinCommand> logger, IUserStorage userStorage) : UserUniversalCommandHandler<JoinCommand>(logger, userStorage)
{   
    protected override async ValueTask Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
        Domain.User domainUser, long chatId)
    {
        domainUser.OptIn();

        await botClient.SendMessage(chatId, Texts.ChooseOnlineOrOffline(Commands.Commands.Online, Commands.Commands.Offline),
            replyMarkup: new InlineKeyboardMarkup(Commands.Commands.Online.Button(chatId), Commands.Commands.Offline.Button(chatId)), 
            cancellationToken: cancellationToken);
    }

}