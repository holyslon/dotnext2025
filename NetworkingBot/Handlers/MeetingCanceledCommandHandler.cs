using NetworkingBot.Commands;
using NetworkingBot.Domain;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class MeetingCanceledCommandHandler(ILogger<MeetingCanceledCommand> logger, IMeetingStorage meetingStorage)
    : MeetingUniversalCommandHandler<MeetingCanceledCommand>(logger, meetingStorage)
{

    protected override async ValueTask Handle(ITelegramBotClient bot, CancellationToken cancellationToken, Meeting meeting, long sourceChatId)
    {
        await meeting.Cancel(cancellationToken);
        
        await bot.SendMessage(meeting.One.ChatId, Texts.MeetingCanceled(
                Commands.Commands.ReadyForMeeting,
                Commands.Commands.Postpone),
            Texts.MatchMessage.ParseMode,
            replyMarkup: new InlineKeyboardMarkup(
                Commands.Commands.ReadyForMeeting.Button(meeting.One.ChatId),
                Commands.Commands.Postpone.Button(meeting.One.ChatId)
            ),
            cancellationToken: cancellationToken);
        await bot.SendMessage(meeting.Another.ChatId, Texts.MeetingCanceled(
                Commands.Commands.ReadyForMeeting,
                Commands.Commands.Postpone),
            Texts.MatchMessage.ParseMode,
            replyMarkup: new InlineKeyboardMarkup(
                Commands.Commands.ReadyForMeeting.Button(meeting.Another.ChatId),
                Commands.Commands.Postpone.Button(meeting.Another.ChatId)
            ),
            cancellationToken: cancellationToken);
        
    }
}