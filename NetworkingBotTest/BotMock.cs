using System.Text.Json;
using NetworkingBot.Commands;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkBotTest;

internal class BotMock : ITelegramBotClient
{
    private readonly List<object> _requests = [];
    private readonly SendPoolAssert _sendPoolAssert = new();

    public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = new())
    {
        if (_sendPoolAssert.TryHandleRequest(request, out var response)) return Task.FromResult(response);
        _requests.Add(request);
        return Task.FromResult((TResponse)default!);
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


    public class SendPoolAssert
    {
        private readonly List<SendPollRequest> _requests = [];
        private long? _chatId;
        private string? _question;
        private readonly List<InputPollOption> _options = new();
        private readonly List<InlineKeyboardButton> _inlineKeyboardButtons = new();
        public string LastPollId { get; private set; } = string.Empty;
        public PollOption[] LastPollOption { get; private set; } = [];

        public SendPoolAssert ForChat(long chatId)
        {
            _chatId = chatId;
            return this;
        }

        internal bool TryHandleRequest<TResponse>(IRequest<TResponse> request, out TResponse response)
        {
            response = default!;
            if (request is not SendPollRequest sendPollRequest || typeof(TResponse) != typeof(Message)) return false;
            _requests.Add(sendPollRequest);
            LastPollId = Guid.NewGuid().ToString();
            LastPollOption = sendPollRequest.Options.Select(o => new PollOption { Text = o.Text, VoterCount = 0 })
                .ToArray();
            var message = new Message
            {
                Chat = new Chat
                {
                    Id = sendPollRequest.ChatId.Identifier ?? 0,
                    Type = ChatType.Private
                },
                Poll = new Poll
                {
                    Id = LastPollId,
                    Type = PollType.Regular,
                    Options = LastPollOption
                }
            };
            var gResponse = JsonSerializer.Serialize(message);
            response = JsonSerializer.Deserialize<TResponse>(gResponse)!;
            return true;
        }

        public void WasSend()
        {
            Assert.Contains(_requests, req =>
            {
                if (_chatId != null && req.ChatId != _chatId) return false;

                if (_question != null && !req.Question.Equals(_question, StringComparison.InvariantCulture))
                    return false;

                if (!_options.All(inputPollOption =>
                        req.Options.Any(o =>
                            o.Text.Equals(inputPollOption.Text, StringComparison.InvariantCulture))))
                    return false;

                foreach (var inlineCallbackButton in _inlineKeyboardButtons)
                    if (req.ReplyMarkup is InlineKeyboardMarkup inlineKeyboardMarkup)
                    {
                        var data = inlineKeyboardMarkup.InlineKeyboard.SelectMany(s => s).ToArray();
                        var found = data.Where(button => button.Text == inlineCallbackButton.Text).Any(button =>
                            button.CallbackData == inlineCallbackButton.CallbackData);
                        if (!found) return false;
                    }
                    else
                    {
                        return false;
                    }

                return true;
            });
        }

        public SendPoolAssert WithQuestion(string question)
        {
            _question = question;
            return this;
        }

        public SendPoolAssert WithAnsverOption(string option)
        {
            _options.Add(new InputPollOption(option));
            return this;
        }

        public SendPoolAssert WithInlineButton(IInlineCommand command, Chat? chat = null)
        {
            chat ??= Create.Chat();
            var button = command.Button(chat);
            _inlineKeyboardButtons.Add(button);
            return this;
        }

        public SendPoolAssert WithInlineButton(InlineKeyboardButton inlineButton)
        {
            _inlineKeyboardButtons.Add(inlineButton);
            return this;
        }
    }

    public class SendMessageRequestAssert(BotMock botMock)
    {
        private long? _chatId;
        private string? _text;
        private ParseMode? _parseMode;
        private readonly List<(string text, string callback)> _inlineCallbackButtons = [];

        public SendMessageRequestAssert ForChat(long chatId)
        {
            _chatId = chatId;
            return this;
        }

        public SendMessageRequestAssert ForChat(Chat chat)
        {
            _chatId = chat.Id;
            return this;
        }

        public SendMessageRequestAssert WithText(string text)
        {
            _text = text;
            return this;
        }

        public SendMessageRequestAssert WithInlineCallback(string text, string callback)
        {
            _inlineCallbackButtons.Add((text, callback));
            return this;
        }

        public SendMessageRequestAssert WithInlineCallback(IInlineCommand command, Chat? chat = null)
        {
            chat ??= Create.Chat();
            var button = command.Button(chat);
            _inlineCallbackButtons.Add((button.Text, button.CallbackData!));
            return this;
        }

        public SendMessageRequestAssert WithParseMode(ParseMode mode)
        {
            _parseMode = mode;
            return this;
        }

        public void WasSend()
        {
            Assert.Contains(botMock._requests, MatchRequest);
        }

        public void WasNotSend()
        {
            Assert.DoesNotContain(botMock._requests, MatchRequest);
        }

        private bool MatchRequest(object req)
        {
            if (req is SendMessageRequest sendMessageRequest)
            {
                if (_chatId != null && sendMessageRequest.ChatId != _chatId) return false;

                if (_text != null && sendMessageRequest.Text != _text) return false;

                if (_parseMode.HasValue && _parseMode.Value != sendMessageRequest.ParseMode) return false;
                foreach (var inlineCallbackButton in _inlineCallbackButtons)
                    if (sendMessageRequest.ReplyMarkup is InlineKeyboardMarkup inlineKeyboardMarkup)
                    {
                        var data = inlineKeyboardMarkup.InlineKeyboard.SelectMany(s => s).ToArray();
                        var found = data.Where(button => button.Text == inlineCallbackButton.text)
                            .Any(button => button.CallbackData == inlineCallbackButton.callback);
                        if (!found) return false;
                    }
                    else
                    {
                        return false;
                    }

                return true;
            }

            return false;
        }
    }

    public SendMessageRequestAssert Message()
    {
        return new SendMessageRequestAssert(this);
    }

    public SendPoolAssert Pool()
    {
        return _sendPoolAssert;
    }
}