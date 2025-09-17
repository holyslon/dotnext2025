using NetworkingBot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class MeetingFeedbackMessageHandler(IMeetingStorage meetingStorage, ILogger<MeetingFeedbackMessageHandler> logger) : ITelegramEventHandler<Message>
{
    public async ValueTask<bool> OnEvent(ITelegramBotClient bot, Message eventPayload, CancellationToken cancellationToken)
    {
        using var _ = logger.BeginScope(new { eventPayload });
        if (eventPayload.From == null)
        {
            logger.LogError("From is null");
            return false;
        }
        return await meetingStorage.WithMeetingForUser(eventPayload.Chat, eventPayload.From, async meeting =>
        {
            if (meeting.IsCompleted && meeting.Source.FeedbackAvailible)
            {
                await meeting.SubmitFeedback(eventPayload.Text, cancellationToken);
                await bot.SendMessage(eventPayload.Chat.Id, Texts.ThankYouForFeedBack(
                        Commands.Commands.ReadyForMeeting,
                        Commands.Commands.Postpone),
                    Texts.MatchMessage.ParseMode,
                    replyMarkup: new InlineKeyboardMarkup(
                        Commands.Commands.ReadyForMeeting.Button(eventPayload.Chat.Id),
                        Commands.Commands.Postpone.Button(eventPayload.Chat.Id)
                    ), cancellationToken: cancellationToken);
            }
            return true;
        }, cancellationToken);
    }
}