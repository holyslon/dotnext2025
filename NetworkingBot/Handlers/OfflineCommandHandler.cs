using System.Collections.Immutable;
using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;
using Poll = NetworkingBot.Domain.Poll;

namespace NetworkingBot.Handlers;

internal class OfflineCommandHandler(ILogger<OfflineCommand> logger, IUserStorage userStorage, IConversationTopicStorage topicStorage, IPollStorage pollStorage)  : UserUniversalCommandHandler<OfflineCommand>(logger, userStorage)
{
    protected override async  ValueTask Handle(ITelegramBotClient botClient, CancellationToken cancellationToken,
        Domain.User domainUser, long chatId)
    {
        domainUser.OfflineParticipation();

        var topicStorageTopics = topicStorage.Topics.ToImmutableArray();
        var response = await botClient.SendPoll(chatId, Texts.ChooseYourInterests(),
            [..topicStorageTopics.Select(t => new InputPollOption(t.Name))], allowsMultipleAnswers: true,
            cancellationToken: cancellationToken);
        if (response.Poll != null)
        {
            var poll = new Poll(domainUser, response.Poll.Id, [..response.Poll.Options.Index().Select(i=>topicStorageTopics[i.Index])]);
            await pollStorage.Save(poll, cancellationToken);
        }
    }
}