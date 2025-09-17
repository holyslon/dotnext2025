using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace NetworkingBot.Handlers;

internal class PostponeCommandHandler(ILogger<PostponeCommand> logger, IUserStorage userStorage)
    : UserUniversalCommandHandler<PostponeCommand>(logger, userStorage)
{
    protected override async ValueTask<bool> Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
        Domain.User domainUser, long chatId)
    {
        domainUser.OptOut();

        await botClient.SendMessage(chatId, 
            Texts.WaitingForYouToReturn(Commands.Commands.Join),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
        return true;
    }
}