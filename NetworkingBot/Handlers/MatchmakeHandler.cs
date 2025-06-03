using NetworkingBot.Conversations;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NetworkingBot.Handlers;

internal class MatchmakeHandler(ConversationDb db, ILogger<JoinHandler> logger) : ITelegramEventHandler<Message>
{
    public Task OnEvent(ITelegramBotClient bot, Message eventPayload, CancellationToken cancellationToken)
    {
        return eventPayload.OnCommand(Command.Matchmake, async () =>
        {
            var participants = db.Conversations.Where(c => c.State == UserState.WhantsCoffe).ToArray();
            var otherParticipants = participants.ToList().Shuffle();
            foreach (var conversation in participants)
            {
                var partner = participants.Index().FirstOrDefault((c) => conversation.ChatId != c.Item.ChatId);
                otherParticipants.RemoveAt(partner.Index);
                await bot.SendMessage(conversation.ChatId,
                    $"Hey we find you a pair for coffee, just dm [{partner.Item.UserName}](tg://user?id={partner.Item.UserId})",
                    ParseMode.MarkdownV2);
                await bot.SendMessage(partner.Item.ChatId,
                    $"Hey we find you a pair for coffee, just dm [{conversation.UserName}](tg://user?id={conversation.UserId})",
                    ParseMode.MarkdownV2);
            }
        });
    }
}