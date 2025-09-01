using NetworkingBot.Commands;
using NetworkingBot.Domain;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class MeetingCanceledCommandHandler(ILogger<MeetingCanceledCommand> logger, IUserStorage userStorage)
    : UserUniversalCommandHandler<MeetingCanceledCommand>(logger, userStorage)
{
    protected override async ValueTask Handle(ITelegramBotClient bot, CancellationToken cancellationToken,
        User domainUser, long chatId)
    {
        domainUser.MeetingCanceled();
        await bot.SendMessage(chatId, Texts.MeetingCanceled(
                Commands.Commands.ReadyForMeeting,
                Commands.Commands.Postpone),
            Texts.MatchMessage.ParseMode,
            replyMarkup: new InlineKeyboardMarkup(
                Commands.Commands.ReadyForMeeting.Button(chatId),
                Commands.Commands.Postpone.Button(chatId)
            ),
            cancellationToken: cancellationToken);
    }
}