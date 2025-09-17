using Telegram.Bot;
using Telegram.Bot.Types;

namespace NetworkingBot.Handlers;

internal class MeetingMessageHandler(IMeetingStorage meetingStorage, ILogger<MeetingMessageHandler> logger) : ITelegramEventHandler<Message>
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
            if (meeting.InProgress)
            {
                foreach (var otherUser in meeting.OtherUsers)
                {
                    await bot.SendMessage(otherUser.ChatId, 
                        Texts.MessageFromUser.Text(meeting.Source.LinkData, eventPayload.Text), 
                        Texts.MessageFromUser.ParseMode, 
                        cancellationToken: cancellationToken);
                }
            }
            return true;
        }, cancellationToken);
    }
}