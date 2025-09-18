using Microsoft.Extensions.Options;
using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Handlers;

internal class ReadyForMeetingCommand(ILogger<ReadyForMeeting> logger, 
    IUserStorage userStorage,
    IMatchService matchService, 
    IOptionsSnapshot<ServiceCollectionExtensions.AppOptions> appOptions)
    : UserUniversalCommandHandler<ReadyForMeeting>(logger, userStorage)
{
    protected override async ValueTask<bool> Handle(ITelegramBotClient bot, CancellationToken cancellationToken,
        Domain.IUser domainUser, long chatId)
    {
        if (domainUser.TryReadyToParticipate())
        {

            await bot.SendMessage(chatId, Texts.WaitForNextMatch(Commands.Commands.Postpone),
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(Commands.Commands.Postpone.Button(chatId)),
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
        return false;
    }
}