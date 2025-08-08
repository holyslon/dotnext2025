using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace NetworkingBot.Infrastructure;

public class TelegramBot(IOptions<TelegramOptions> options)
    : ITelegramBotClient
{
    private readonly TelegramBotClient _botClient = new(options.Value.Token!);


    public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = new())
    {
        return _botClient.SendRequest(request, cancellationToken);
    }

    public Task<bool> TestApi(CancellationToken cancellationToken = new())
    {
        return _botClient.TestApi(cancellationToken);
    }

    public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = new())
    {
        return _botClient.DownloadFile(filePath, destination, cancellationToken);
    }

    public Task DownloadFile(TGFile file, Stream destination, CancellationToken cancellationToken = new())
    {
        return _botClient.DownloadFile(file, destination, cancellationToken);
    }

    public bool LocalBotServer => _botClient.LocalBotServer;

    public long BotId => _botClient.BotId;

    public TimeSpan Timeout
    {
        get => _botClient.Timeout;
        set => _botClient.Timeout = value;
    }

    public IExceptionParser ExceptionsParser
    {
        get => _botClient.ExceptionsParser;
        set => _botClient.ExceptionsParser = value;
    }

    public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest
    {
        add => _botClient.OnMakingApiRequest += value;
        remove => _botClient.OnMakingApiRequest -= value;
    }

    public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived
    {
        add => _botClient.OnApiResponseReceived += value;
        remove => _botClient.OnApiResponseReceived -= value;
    }
}