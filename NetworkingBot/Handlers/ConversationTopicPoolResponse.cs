using System.Collections.Immutable;
using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class ConversationTopicPoolResponse(ILogger<OnlineCommand> logger, IUserStorage userStorage, IPollStorage pollStorage, IMatchService matchService) : ITelegramEventHandler<Poll>
{
    public async ValueTask OnEvent(ITelegramBotClient bot, Poll eventPayload, CancellationToken cancellationToken)
    {
        using var _ = logger.BeginScope(new {eventPayload.Id});
        var domainPoll = await pollStorage.GetById(eventPayload.Id,  cancellationToken);
            
        var topics = eventPayload.Options.Index().Where(i=>i.Item.VoterCount > 0)
            .Select(opt=> domainPoll.Topics[opt.Index])
            .ToImmutableArray();

        var domainUser = domainPoll.DomainUser;
        domainUser.SetConversationTopics(topics);
        domainUser.ReadyToParticipate();
        await userStorage.Save(domainUser, cancellationToken);
        var id = domainUser.Id;
        
        await bot.SendMessage(id.ChatId,Texts.WaitForNextMatch(Commands.Commands.Postpone),
            replyMarkup: new InlineKeyboardMarkup(Commands.Commands.Postpone.Button(id.ChatId)), cancellationToken: cancellationToken);

        if (domainUser.TryGetSearchInfo(out var info))
        {
            var (found, match) = await matchService.TryFindMatch(info);
            if (found && match != null)
            {
                await userStorage.Save(match.One, cancellationToken);
                await userStorage.Save(match.Another, cancellationToken);

                await bot.SendMessage(match.One.Id.ChatId, Texts.MatchMessage.Text(match.Another.Link,
                        Commands.Commands.MeetingHappenCommand,
                        Commands.Commands.MeetingCanceledCommand),
                    Texts.MatchMessage.ParseMode,
                    replyMarkup: new InlineKeyboardMarkup(
                        Commands.Commands.MeetingHappenCommand.Button(match.One.Id.ChatId),
                        Commands.Commands.MeetingCanceledCommand.Button(match.One.Id.ChatId)
                    ),
                    cancellationToken: cancellationToken);
                await bot.SendMessage(match.Another.Id.ChatId, Texts.MatchMessage.Text(match.One.Link,
                        Commands.Commands.MeetingHappenCommand,
                        Commands.Commands.MeetingCanceledCommand),
                    Texts.MatchMessage.ParseMode,
                    replyMarkup: new InlineKeyboardMarkup(
                        Commands.Commands.MeetingHappenCommand.Button(match.Another.Id.ChatId),
                        Commands.Commands.MeetingCanceledCommand.Button(match.Another.Id.ChatId)
                    ),
                    cancellationToken: cancellationToken);
            }

        }
        
    }
}