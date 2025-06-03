using Telegram.Bot.Types;

namespace NetworkingBot.Handlers;

internal static class CallbackQueryExtensions
{
    internal class DataHandler(ILogger logger)
    {
        private readonly Dictionary<string, Func<long, Task>> _callbacks = new();

        public DataHandler OnData(string key, Func<long, Task> callback)
        {
            _callbacks[key] = callback;
            return this;
        }

        public Task Do(long chatId, string data)
        {
            using var _ = logger.BeginScope(new { data });
            if (_callbacks.TryGetValue(data, out var callback)) return callback(chatId);
            logger.LogInformation("Cant parse action");
            return Task.CompletedTask;
        }
    }

    internal static Task OnCallback(this CallbackQuery callbackQuery, ILogger logger, Action<DataHandler> action)
    {
        var dataHandler = new DataHandler(logger);
        action(dataHandler);
        return callbackQuery.OnCallback(logger, dataHandler.Do);
    }

    internal static Task OnCallback(this CallbackQuery callbackQuery, ILogger logger, Func<long, string, Task> action)
    {
        var data = callbackQuery.Data;
        using var _ = logger.BeginScope(new { data });
        var items = data?.Split(":") ?? [];
        if (items.Length != 2)
        {
            logger.LogInformation("Unknown callback data format");
            return Task.CompletedTask;
        }

        if (!long.TryParse(items[0], out var chatId))
        {
            logger.LogInformation("Cant parse chat id");
            return Task.CompletedTask;
        }

        return action(chatId, items[1]);
    }
}