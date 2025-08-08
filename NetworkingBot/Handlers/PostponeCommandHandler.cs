using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;

namespace NetworkingBot.Handlers
{
    internal class PostponeCommandHandler(ILogger<PostponeCommand> logger, IUserStorage userStorage)  : UserUniversalCommandHandler<PostponeCommand>(logger, userStorage)
    {
        protected override async ValueTask Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
            Domain.User domainUser, long chatId)
        {
            domainUser.OptOut();

            await botClient.SendMessage(chatId, Texts.WaitingForYouToReturn(Commands.Commands.Join),
                cancellationToken: cancellationToken);
        }
    }
}