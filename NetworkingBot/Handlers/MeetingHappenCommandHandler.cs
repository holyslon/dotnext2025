using NetworkingBot.Commands;
using NetworkingBot.Domain;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class MeetingHappenCommandHandler(ILogger<MeetingHappenCommand> logger, IUserStorage userStorage)
    : UserUniversalCommandHandler<MeetingHappenCommand>(logger, userStorage)
{
    protected override async ValueTask Handle(ITelegramBotClient bot, CancellationToken cancellationToken, User domainUser, long chatId)
    {
        domainUser.MeetingCompleted();
        await bot.SendMessage(chatId, Texts.MeetingCompleted(
                Commands.Commands.ReadyForMeeting, 
                Commands.Commands.Postpone), 
            Texts.MatchMessage.ParseMode, 
            replyMarkup:  new InlineKeyboardMarkup(
                Commands.Commands.ReadyForMeeting.Button(chatId),
                Commands.Commands.Postpone.Button(chatId)
            ),
            cancellationToken:cancellationToken);
    }
}