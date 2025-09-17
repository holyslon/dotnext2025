using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class ConversationTopicPoolResponse(
    ILogger<OnlineCommand> logger,
    IUserStorage userStorage,
    IPollStorage pollStorage,
    IMatchService matchService, 
    IOptionsSnapshot<ServiceCollectionExtensions.AppOptions> appOptions) : ITelegramEventHandler<Poll>
{
    public async ValueTask<bool> OnEvent(ITelegramBotClient bot, Poll eventPayload, CancellationToken cancellationToken)
    {
        using var _ = logger.BeginScope(new { eventPayload.Id });
        var domainPoll = await pollStorage.GetById(eventPayload.Id, cancellationToken);

        var topics = eventPayload.Options.Index().Where(i => i.Item.VoterCount > 0)
            .Select(opt => domainPoll.Topics[opt.Index])
            .ToImmutableArray();

        var domainUser = domainPoll.DomainUser;
        domainUser.SetConversationTopics(topics);
        domainUser.ReadyToParticipate();
        await userStorage.Save(domainUser, cancellationToken);
        var id = domainUser.Id;

        await bot.SendMessage(id.ChatId, Texts.WaitForNextMatch(Commands.Commands.Postpone),
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(Commands.Commands.Postpone.Button(id.ChatId)),
            cancellationToken: cancellationToken);

        if (domainUser.TryGetSearchInfo(out var info))
        {
            var (found, match) = await matchService.TryFindMatch(info, cancellationToken);
            if (found && match != null)
            {
                var baseUrl = appOptions.Value.BaseUrl;
                await bot.SendMessage(match.One.ChatId, Texts.MatchMessage.Text(baseUrl,
                        match.Another.LinkData,
                        Commands.Commands.MeetingHappenCommand,
                        Commands.Commands.MeetingCanceledCommand),
                    Texts.MatchMessage.ParseMode,
                    replyMarkup: new InlineKeyboardMarkup(
                        Commands.Commands.MeetingHappenCommand.Button(match.One.ChatId),
                        Commands.Commands.MeetingCanceledCommand.Button(match.One.ChatId)
                    ),
                    cancellationToken: cancellationToken);
                await bot.SendMessage(match.Another.ChatId, Texts.MatchMessage.Text(baseUrl,
                    match.One.LinkData,
                        Commands.Commands.MeetingHappenCommand,
                        Commands.Commands.MeetingCanceledCommand),
                    Texts.MatchMessage.ParseMode,
                    replyMarkup: new InlineKeyboardMarkup(
                        Commands.Commands.MeetingHappenCommand.Button(match.Another.ChatId),
                        Commands.Commands.MeetingCanceledCommand.Button(match.Another.ChatId)
                    ),
                    cancellationToken: cancellationToken);
            }
        }
        return true;
    }
}