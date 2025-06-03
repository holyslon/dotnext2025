using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetworkingBot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Xunit.Abstractions;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

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

    private IUpdateHandler UpdateHandler => _serviceProvider.GetRequiredService<IUpdateHandler>();
    private IMatchmakingService MatchmakingService => _serviceProvider.GetRequiredService<IMatchmakingService>();
    private BotMoock BotClient { get; } = new();


    private const long DefaultChatId = 12312;
    private const long DefaultUserId = 3452354;
    private const string DefaultUsername = "John Doe";

    private User User(long id = DefaultUserId, string username = DefaultUsername)
    {
        return new User
        {
            Id = id,
            Username = username
        };
    }

    private Chat Chat(long id = DefaultChatId)
    {
        return new Chat
        {
            Id = id
        };
    }

    private Message Message(string text, Chat? chat = null, User? user = null)
    {
        return new Message
        {
            Text = text,
            Chat = chat ?? Chat(),
            From = user ?? User()
        };
    }

    private CallbackQuery CallbackQuery(string data)
    {
        return new CallbackQuery
        {
            Data = data
        };
    }


    private class BotMoock : ITelegramBotClient
    {
        private readonly List<object> _requests = new();

        public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request,
            CancellationToken cancellationToken = new())
        {
            _requests.Add(request);
            return Task.FromResult((TResponse)default);
        }

        public Task<bool> TestApi(CancellationToken cancellationToken = new())
        {
            return Task.FromResult(true);
        }

        public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = new())
        {
            return Task.CompletedTask;
        }

        public Task DownloadFile(TGFile file, Stream destination, CancellationToken cancellationToken = new())
        {
            return Task.CompletedTask;
        }

        public bool LocalBotServer { get; } = false;
        public long BotId { get; } = 0;
        public TimeSpan Timeout { get; set; }
        public IExceptionParser ExceptionsParser { get; set; } = new DefaultExceptionParser();
        public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;
        public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;

        public class SendMessageRequestAssert
        {
            private readonly BotMoock _botMoock;

            private long? chatId = null;
            private string? text = null;

            public SendMessageRequestAssert(BotMoock botMoock)
            {
                _botMoock = botMoock;
            }

            public SendMessageRequestAssert ForChat(long chatId)
            {
                this.chatId = chatId;
                return this;
            }

            public SendMessageRequestAssert WithText(string text)
            {
                this.text = text;
                return this;
            }

            public void WasSend()
            {
                Assert.Contains(_botMoock._requests, req =>
                {
                    if (req is SendMessageRequest sendMessageRequest)
                        return (chatId == null || sendMessageRequest.ChatId == chatId)
                               && (text == null ||
                                   sendMessageRequest.Text.Equals(text, StringComparison.InvariantCulture));

                    return false;
                });
            }
        }

        public SendMessageRequestAssert Message()
        {
            return new SendMessageRequestAssert(this);
        }
    }

    [Fact]
    public async Task TestThatWeCanStartBot()
    {
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            Message = Message("/start")
        }, CancellationToken.None);

        BotClient.Message().ForChat(DefaultChatId)
            .WithText(
                "Hello there! If you want coffee with someone, just select yes. If you observing just select 'maybe later'")
            .WasSend();
    }

    [Fact]
    public async Task TestThatWeSubscribeUserIfHePressYes()
    {
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            Message = Message("/start")
        }, CancellationToken.None);
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            CallbackQuery = CallbackQuery($"{DefaultChatId}:yes")
        }, CancellationToken.None);

        BotClient.Message().ForChat(DefaultChatId)
            .WithText(
                "Cool we will find you someone to have coffee with! When you want cancel this activity just type '/cancel' to this bot.")
            .WasSend();
    }

    [Fact]
    public async Task TestThatWeUnSubscribeUserIfHeRunsCancel()
    {
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            Message = Message("/start")
        }, CancellationToken.None);
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            CallbackQuery = CallbackQuery($"{DefaultChatId}:yes")
        }, CancellationToken.None);
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            Message = Message("/cancel")
        }, CancellationToken.None);
        BotClient.Message().ForChat(DefaultChatId)
            .WithText("Ok. Nice to meet you. If you want join to random coffee later, just type '/join' to this bot.")
            .WasSend();
    }

    [Fact]
    public async Task TestThatWeNotSubscribeUserIfHePressMayBeLater()
    {
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            Message = Message("/start")
        }, CancellationToken.None);
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            CallbackQuery = CallbackQuery($"{DefaultChatId}:postpone")
        }, CancellationToken.None);

        BotClient.Message().ForChat(DefaultChatId)
            .WithText("Ok. Nice to meet you. If you want join to random coffee later, just type '/join' to this bot.")
            .WasSend();
    }

    [Fact]
    public async Task TestThatWeSubscribeUserIfHeEnterJoin()
    {
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            Message = Message("/start")
        }, CancellationToken.None);
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            CallbackQuery = CallbackQuery($"{DefaultChatId}:postpone")
        }, CancellationToken.None);
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            Message = Message("/join")
        }, CancellationToken.None);
        BotClient.Message().ForChat(DefaultChatId)
            .WithText(
                "Cool we will find you someone to have coffee with! When you want cancel this activity just type '/cancel' to this bot.")
            .WasSend();
    }

    private async Task RegisterUser(long chatId, long userId, string name)
    {
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            Message = Message("/start", Chat(chatId), User(userId, name))
        }, CancellationToken.None);
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            CallbackQuery = CallbackQuery($"{chatId}:yes")
        }, CancellationToken.None);
    }

    [Fact]
    public async Task TestThatWeCanMatchmakeUser()
    {
        await RegisterUser(255234, 134324, "John");
        await RegisterUser(25523421, 13432432, "Marry");

        await MatchmakingService.CreateEventFor(134324, BotClient, CancellationToken.None);

        BotClient.Message().ForChat(255234)
            .WithText(
                "Hey we find you a pair for coffee, just dm [Marry](tg://user?id=13432432). When you ready for another round just press 'ready for another coffee' or type '/ready_for_coffee'.")
            .WasSend();
        BotClient.Message().ForChat(25523421)
            .WithText(
                "Hey we find you a pair for coffee, just dm [John](tg://user?id=134324). When you ready for another round just press 'ready for another coffee' or type '/ready_for_coffee'.")
            .WasSend();
    }

    
    [Fact]
    public async Task TestThatMatchmakedUserCanReturnToThePool()
    {
        await RegisterUser(255234, 134324, "John");
        await RegisterUser(25523421, 13432432, "Marry");

        await MatchmakingService.CreateEventFor(134324, BotClient, CancellationToken.None);
        
        await UpdateHandler.HandleUpdateAsync(BotClient, new Update
        {
            CallbackQuery = CallbackQuery($"{255234}:another")
        }, CancellationToken.None);
        BotClient.Message().ForChat(255234)
            .WithText(
                "We are looking next person to meet. As soon as we find one - we will come back to you")
            .WasSend();
        
    }

    public ValueTask DisposeAsync()
    {
        return _serviceProvider.DisposeAsync();
    }
}