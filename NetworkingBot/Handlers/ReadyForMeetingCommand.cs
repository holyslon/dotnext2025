using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class ReadyForMeetingCommand(ILogger<ReadyForMeeting> logger, IUserStorage userStorage)
    : UserUniversalCommandHandler<ReadyForMeeting>(logger, userStorage)
{
    protected override async ValueTask<bool> Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
        Domain.User domainUser, long chatId)
    {
        domainUser.ReadyToParticipate();

        await botClient.SendMessage(chatId, Texts.WaitForNextMatch(Commands.Commands.Postpone),
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(Commands.Commands.Postpone.Button(chatId)),
            cancellationToken: cancellationToken);
        return true;
    }
}