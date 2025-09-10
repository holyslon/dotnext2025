using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NetworkingBot.Infrastructure;

public class UpdateHandler(ILogger<UpdateHandler> logger, IServiceProvider serviceProvider) : IUpdateHandler
{
    private async Task HandleEvent<T>(ITelegramBotClient bot, T update, CancellationToken cancellationToken)
    {
        using var _ = logger.BeginScope(new { UpdateType = typeof(T).Name });
        await using var scope = serviceProvider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetService<ITelegramEventHandler<T>>();
        if (handler != null) await handler.OnEvent(bot, update, cancellationToken).AsTask();
        logger.LogWarning("Handler for event not found");
    }

    public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        using var _ = logger.BeginScope(new { Update = update });
        return update.Type switch
        {
            UpdateType.Unknown => HandleEvent(botClient, update, cancellationToken),
            UpdateType.Message => HandleEvent(botClient, update.Message!, cancellationToken),
            UpdateType.InlineQuery => HandleEvent(botClient, update.InlineQuery!, cancellationToken),
            UpdateType.ChosenInlineResult => HandleEvent(botClient, update.ChosenInlineResult!, cancellationToken),
            UpdateType.CallbackQuery => HandleEvent(botClient, update.CallbackQuery!, cancellationToken),
            UpdateType.EditedMessage => HandleEvent(botClient, update.EditedMessage!, cancellationToken),
            UpdateType.ChannelPost => HandleEvent(botClient, update.ChannelPost!, cancellationToken),
            UpdateType.EditedChannelPost => HandleEvent(botClient, update.EditedChannelPost, cancellationToken),
            UpdateType.ShippingQuery => HandleEvent(botClient, update.ShippingQuery!, cancellationToken),
            UpdateType.PreCheckoutQuery => HandleEvent(botClient, update.PreCheckoutQuery!, cancellationToken),
            UpdateType.Poll => HandleEvent(botClient, update.Poll!, cancellationToken),
            UpdateType.PollAnswer => HandleEvent(botClient, update.PollAnswer!, cancellationToken),
            UpdateType.MyChatMember => HandleEvent(botClient, update.MyChatMember!, cancellationToken),
            UpdateType.ChatMember => HandleEvent(botClient, update.ChatMember!, cancellationToken),
            UpdateType.ChatJoinRequest => HandleEvent(botClient, update.ChatJoinRequest!, cancellationToken),
            UpdateType.MessageReaction => HandleEvent(botClient, update.MessageReaction!, cancellationToken),
            UpdateType.MessageReactionCount => HandleEvent(botClient, update.MessageReactionCount!, cancellationToken),
            UpdateType.ChatBoost => HandleEvent(botClient, update.ChatBoost!, cancellationToken),
            UpdateType.RemovedChatBoost => HandleEvent(botClient, update.RemovedChatBoost!, cancellationToken),
            UpdateType.BusinessConnection => HandleEvent(botClient, update.BusinessConnection!, cancellationToken),
            UpdateType.BusinessMessage => HandleEvent(botClient, update.BusinessMessage!, cancellationToken),
            UpdateType.EditedBusinessMessage =>
                HandleEvent(botClient, update.EditedBusinessMessage!, cancellationToken),
            UpdateType.DeletedBusinessMessages => HandleEvent(botClient, update.DeletedBusinessMessages!,
                cancellationToken),
            UpdateType.PurchasedPaidMedia => HandleEvent(botClient, update.PurchasedPaidMedia!, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(update.Type))
        };
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        using var _ = logger.BeginScope(new { Source = source });
        logger.LogError(exception, "Fail to handle update");
        return Task.CompletedTask;
    }
}