using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetworkingBot;
using NetworkingBot.Commands;
using NetworkingBot.Infrastructure;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Telegram.Bot.Polling;

namespace NetworkBotTest;

public class HappyPathTests : IAsyncDisposable, IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public HappyPathTests(ITestOutputHelper output)
    {
        var networkingBot = new ServiceCollection()
            .AddLogging(conf => { conf.AddXunit(output); })
            .AddNetworkingBot("Server=127.0.0.1;Port=54321;Userid=postgres;Password=example");
        networkingBot.AddOpenTelemetry()
            .ConfigureResource(rb=>rb.AddService("Test"))
            .WithLogging(opts => opts.AddConsoleExporter(op => op.Targets = ConsoleExporterOutputTargets.Debug))
            .WithMetrics(opts => opts.AddConsoleExporter(op => op.Targets = ConsoleExporterOutputTargets.Debug))
            .WithTracing(opts => opts.AddConsoleExporter(op => op.Targets = ConsoleExporterOutputTargets.Debug));
        BotClient = new BotMock(output);
        _serviceProvider = networkingBot
            .BuildServiceProvider();
        using var serviceScope = _serviceProvider.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
    }

    private UpdateHandlerAndBot UpdateHandler => new(_serviceProvider.GetRequiredService<IUpdateHandler>(), BotClient);

    // private IUpdateHandler UpdateHandler => _serviceProvider.GetRequiredService<IUpdateHandler>();
    // private IMatchmakingService MatchmakingService => _serviceProvider.GetRequiredService<IMatchmakingService>();
    private BotMock BotClient { get; }


    [Fact]
    public async Task TestThatWeCanStartBot()
    {
        await UpdateHandler.HandleUpdateAsync(Create.Update(Create.Message("/start")));

        BotClient.Message().ForChat(Default.ChatId)
            .WithText(Texts.Welcome(Commands.Join, Commands.Postpone))
            .WithInlineCallback(Commands.Join)
            .WithInlineCallback(Commands.Postpone)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWeSendMessageIfUserPostpone()
    {
        await UpdateHandler.HandleUpdateAsync(Create.Update(Create.Message("/start")));
        await UpdateHandler.Update(callback: Commands.Postpone.CallbackQuery());

        BotClient.Message().ForChat(Default.ChatId)
            .WithText(Texts.WaitingForYouToReturn(Commands.Join))
            .WasSend();
    }


    [Fact]
    public async Task TestThatWeAskOnlineOrOfflineIfUserPressYes()
    {
        await UpdateHandler.HandleUpdateAsync(Create.Update(Create.Message("/start")));

        await UpdateHandler.Update(callback: Commands.Join.CallbackQuery());

        BotClient.Message().ForChat(Default.ChatId)
            .WithText(Texts.ChooseOnlineOrOffline(Commands.Online, Commands.Offline))
            .WithInlineCallback(Commands.Online)
            .WithInlineCallback(Commands.Offline)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWeAskForInterestsWhenPickOnline()
    {
        await UpdateHandler.HandleUpdateAsync(Create.Update(Create.Message("/start")));

        await UpdateHandler.Update(callback: Commands.Join.CallbackQuery());
        await UpdateHandler.Update(callback: Commands.Online.CallbackQuery());


        BotClient.Pool().ForChat(Default.ChatId)
            .WithQuestion(Texts.ChooseYourInterests())
            .WithAnsverOption(Interests.PostgresSql)
            .WithAnsverOption(Interests.Async)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWeAskForInterestsWhenPickOffline()
    {
        await UpdateHandler.HandleUpdateAsync(Create.Update(Create.Message("/start")));

        await UpdateHandler.Update(callback: Commands.Join.CallbackQuery());
        await UpdateHandler.Update(callback: Commands.Offline.CallbackQuery());


        BotClient.Pool().ForChat(Default.ChatId)
            .WithQuestion(Texts.ChooseYourInterests())
            .WithAnsverOption(Interests.PostgresSql)
            .WithAnsverOption(Interests.Async)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWhenWePeekInterestWeAreReadyToMatch()
    {
        await UpdateHandler.Update(Create.Message("/start"));

        await UpdateHandler.Update(callback: Commands.Join.CallbackQuery());
        await UpdateHandler.Update(callback: Commands.Offline.CallbackQuery());
        await UpdateHandler.Update(poll: Create.Poll(BotClient.Pool().LastPollId,
            BotClient.Pool().LastPollOption.Vote(Interests.PostgresSql)));


        BotClient.Message().ForChat(Default.ChatId)
            .WithText(Texts.WaitForNextMatch(Commands.Postpone))
            .WithInlineCallback(Commands.Postpone)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWheCanMatch()
    {
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);
        var (secondUser, secondChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);

        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MatchMessage.Text(secondUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, firstChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, firstChat)
            .WasSend();

        BotClient.Message().ForChat(secondChat)
            .WithText(Texts.MatchMessage.Text(firstUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, secondChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, secondChat)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWheCanCancelAfterMatch()
    {
        var (_, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);
        await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);

        await UpdateHandler.Update(callback: Commands.MeetingCanceledCommand.CallbackQuery(firstChat));

        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MeetingCanceled(Commands.ReadyForMeeting, Commands.Postpone))
            .WithInlineCallback(Commands.ReadyForMeeting, firstChat)
            .WithInlineCallback(Commands.Postpone, firstChat)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWheCanCompleteMeetingAfterMatch()
    {
        var (_, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);
        await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);

        await UpdateHandler.Update(callback: Commands.MeetingHappenCommand.CallbackQuery(firstChat));

        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MeetingCompleted(Commands.ReadyForMeeting, Commands.Postpone))
            .WithInlineCallback(Commands.ReadyForMeeting, firstChat)
            .WithInlineCallback(Commands.Postpone, firstChat)
            .WasSend();
    }


    [Fact]
    public async Task TestThatWheCanMatchAfterNewReadyForMeeting()
    {
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);
        await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);

        await UpdateHandler.Update(callback: Commands.MeetingHappenCommand.CallbackQuery(firstChat));
        await UpdateHandler.Update(callback: Commands.ReadyForMeeting.CallbackQuery(firstChat));

        var (secondUser, secondChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);


        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MatchMessage.Text(secondUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, firstChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, firstChat)
            .WasSend();

        BotClient.Message().ForChat(secondChat)
            .WithText(Texts.MatchMessage.Text(firstUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, secondChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, secondChat)
            .WasSend();
    }

    [Fact]
    public async Task TestThatNoMatchHappenAfterPostpone()
    {
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);
        await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);

        await UpdateHandler.Update(callback: Commands.MeetingHappenCommand.CallbackQuery(firstChat));
        await UpdateHandler.Update(callback: Commands.Postpone.CallbackQuery(firstChat));

        var (secondUser, secondChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);


        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MatchMessage.Text(secondUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, firstChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, firstChat)
            .WasNotSend();

        BotClient.Message().ForChat(secondChat)
            .WithText(Texts.MatchMessage.Text(firstUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, secondChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, secondChat)
            .WasNotSend();
    }
    
    [Fact]
    public async Task TestThatMatchPriorityIsForUsersThatWeDontHavePreviousMatches()
    {
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);
        var (firstMeetingUser, firstMeetingChat) =await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);

        await UpdateHandler.Update(callback: Commands.MeetingHappenCommand.CallbackQuery(firstChat));
        await UpdateHandler.Update(callback: Commands.ReadyForMeeting.CallbackQuery(firstChat));
        await UpdateHandler.Update(callback: Commands.ReadyForMeeting.CallbackQuery(firstMeetingChat));

        var (secondUser, secondChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);


        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MatchMessage.Text(secondUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, firstChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, firstChat)
            .WasSend();

        BotClient.Message().ForChat(secondChat)
            .WithText(Texts.MatchMessage.Text(firstUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, secondChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, secondChat)
            .WasSend();
    }

    [Fact] 
    public async Task TestThatNoMatchHappenBetweenOnlineAndOfflineUsers()
    {
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);

        var (secondUser, secondChat) = await UpdateHandler.OfflineUser(Interests.PostgresSql, Interests.Async);


        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MatchMessage.Text(secondUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, firstChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, firstChat)
            .WasNotSend();

        BotClient.Message().ForChat(secondChat)
            .WithText(Texts.MatchMessage.Text(firstUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, secondChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, secondChat)
            .WasNotSend();
    }
    [Fact] 
    public async Task DontMatchWithNotDecidedUser()
    {
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);
        var (secondUser, secondChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);

        await UpdateHandler.Update(callback: Commands.MeetingHappenCommand.CallbackQuery(secondChat));
        await UpdateHandler.Update(callback: Commands.ReadyForMeeting.CallbackQuery(secondChat));

        var (thirdUser, thirdChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);


        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MatchMessage.Text(thirdUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, firstChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, firstChat)
            .WasNotSend();

        BotClient.Message().ForChat(thirdChat)
            .WithText(Texts.MatchMessage.Text(firstUser.LinkData(), Commands.MeetingHappenCommand,
                Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, thirdChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, thirdChat)
            .WasNotSend();
    }
    [Fact] 
    public async Task DoWeSendPostMeetingMessageForBothUsers()
    {
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);
        var (secondUser, secondChat) = await UpdateHandler.OnlineUser(Interests.PostgresSql, Interests.Async);

        await UpdateHandler.Update(callback: Commands.MeetingHappenCommand.CallbackQuery(secondChat));

        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MeetingCompleted(Commands.ReadyForMeeting, Commands.Postpone))
            .WithInlineCallback(Commands.ReadyForMeeting, firstChat)
            .WithInlineCallback(Commands.Postpone, firstChat)
            .WasSend();

        BotClient.Message().ForChat(secondChat)
            .WithText(Texts.MeetingCompleted(Commands.ReadyForMeeting, Commands.Postpone))
            .WithInlineCallback(Commands.ReadyForMeeting, secondChat)
            .WithInlineCallback(Commands.Postpone, secondChat)
            .WasSend();
    }


    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.GetRequiredService<IApplicationClearer>().Clear();
        await _serviceProvider.DisposeAsync();
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }
}