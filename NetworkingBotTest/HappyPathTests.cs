using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetworkingBot;
using NetworkingBot.Commands;
using Telegram.Bot.Polling;
using Xunit.Abstractions;

namespace NetworkBotTest;

public class HappyPathTests : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public HappyPathTests(ITestOutputHelper output)
    {
        _serviceProvider = new ServiceCollection()
            .AddLogging(conf => { conf.AddXunit(output); })
            .AddNetworkingBot()
            .BuildServiceProvider();
    }

    private UpdateHandlerAndBot UpdateHandler => new UpdateHandlerAndBot(_serviceProvider.GetRequiredService<IUpdateHandler>(), BotClient);
    
    // private IUpdateHandler UpdateHandler => _serviceProvider.GetRequiredService<IUpdateHandler>();
    // private IMatchmakingService MatchmakingService => _serviceProvider.GetRequiredService<IMatchmakingService>();
    private BotMock BotClient { get; } = new();
    


    [Fact]
    public async Task TestThatWeCanStartBot()
    {
        await UpdateHandler.HandleUpdateAsync(Create.Update(message: Create.Message("/start")));

        BotClient.Message().ForChat(Default.ChatId)
            .WithText(Texts.Welcome( Commands.Join, Commands.Postpone))
            .WithInlineCallback( Commands.Join)
            .WithInlineCallback(Commands.Postpone)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWeSendMessageIfUserPostpone()
    {
        await UpdateHandler.HandleUpdateAsync(Create.Update(message: Create.Message("/start")));
        await UpdateHandler.Update(callback: Commands.Postpone.CallbackQuery());

        BotClient.Message().ForChat(Default.ChatId)
            .WithText(Texts.WaitingForYouToReturn( Commands.Join))
            .WasSend();
    }
    

    [Fact]
    public async Task TestThatWeAskOnlineOrOfflineIfUserPressYes()
    {
        await UpdateHandler.HandleUpdateAsync(Create.Update(message: Create.Message("/start")));

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
        await UpdateHandler.HandleUpdateAsync(Create.Update(message: Create.Message("/start")));
    
        await UpdateHandler.Update(callback: Commands.Join.CallbackQuery());
        await UpdateHandler.Update(callback: Commands.Online.CallbackQuery());
        
        
        BotClient.Pool().ForChat(Default.ChatId)
            .WithQuestion(Texts.ChooseYourInterests())
            .WithAnsverOption(Interests.PostgreSql)
            .WithAnsverOption(Interests.Async)
            .WasSend();
    }
    
    [Fact]
    public async Task TestThatWeAskForInterestsWhenPickOffline()
    {
        await UpdateHandler.HandleUpdateAsync(Create.Update(message: Create.Message("/start")));
    
        await UpdateHandler.Update(callback: Commands.Join.CallbackQuery());
        await UpdateHandler.Update(callback: Commands.Offline.CallbackQuery());
        
        
        BotClient.Pool().ForChat(Default.ChatId)
            .WithQuestion(Texts.ChooseYourInterests())
            .WithAnsverOption(Interests.PostgreSql)
            .WithAnsverOption(Interests.Async)
            .WasSend();
    }
    
    [Fact]
    public async Task TestThatWhenWePeekInterestWeAreReadyToMatch()
    {
        await UpdateHandler.Update(message: Create.Message("/start"));
    
        await UpdateHandler.Update(callback: Commands.Join.CallbackQuery());
        await UpdateHandler.Update(callback: Commands.Offline.CallbackQuery());
        await UpdateHandler.Update(poll: Create.Poll(BotClient.Pool().LastPollId, 
            BotClient.Pool().LastPollOption.Vote(Interests.PostgreSql)));
        
        
        BotClient.Message().ForChat(Default.ChatId)
            .WithText(Texts.WaitForNextMatch(Commands.Postpone))
            .WithInlineCallback(Commands.Postpone)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWheCanMatch()
    {
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        var (secondUser, secondChat) = await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        
        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MatchMessage.Text(secondUser.LinkData(), Commands.MeetingHappenCommand, Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, firstChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, firstChat)
            .WasSend();

        BotClient.Message().ForChat(secondChat)
            .WithText(Texts.MatchMessage.Text(firstUser.LinkData(), Commands.MeetingHappenCommand, Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, secondChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, secondChat)
            .WasSend();
    }

    [Fact]
    public async Task TestThatWheCanCancelAfterMatch()
    {
        var (_, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        
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
        var (_, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        
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
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        
        await UpdateHandler.Update(callback: Commands.MeetingHappenCommand.CallbackQuery(firstChat));
        await UpdateHandler.Update(callback: Commands.ReadyForMeeting.CallbackQuery(firstChat));
        
        var (secondUser, secondChat) = await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        
        
        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MatchMessage.Text(secondUser.LinkData(), Commands.MeetingHappenCommand, Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, firstChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, firstChat)
            .WasSend();

        BotClient.Message().ForChat(secondChat)
            .WithText(Texts.MatchMessage.Text(firstUser.LinkData(), Commands.MeetingHappenCommand, Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, secondChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, secondChat)
            .WasSend();
    }
    
    [Fact]
    public async Task TestThatNoMatchHappenAfterPostpone()
    {
        var (firstUser, firstChat) = await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        
        await UpdateHandler.Update(callback: Commands.MeetingHappenCommand.CallbackQuery(firstChat));
        await UpdateHandler.Update(callback: Commands.Postpone.CallbackQuery(firstChat));
        
        var (secondUser, secondChat) = await UpdateHandler.OnlineUser(Interests.PostgreSql, Interests.Async);
        
        
        BotClient.Message().ForChat(firstChat)
            .WithText(Texts.MatchMessage.Text(secondUser.LinkData(), Commands.MeetingHappenCommand, Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, firstChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, firstChat)
            .WasNotSend();

        BotClient.Message().ForChat(secondChat)
            .WithText(Texts.MatchMessage.Text(firstUser.LinkData(), Commands.MeetingHappenCommand, Commands.MeetingCanceledCommand))
            .WithParseMode(Texts.MatchMessage.ParseMode)
            .WithInlineCallback(Commands.MeetingHappenCommand, secondChat)
            .WithInlineCallback(Commands.MeetingCanceledCommand, secondChat)
            .WasNotSend();
    }
    
    public ValueTask DisposeAsync()
    {
        return _serviceProvider.DisposeAsync();
    }
}