using NetworkingBot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NetworkingBot.Handlers;

internal interface ISlashCommandHandler<in TCommand>: ITelegramEventHandler<Message> where TCommand : ISlashCommand, new() 
{
    ValueTask HandleAsync(ITelegramBotClient botClient, Chat chat, User? user, TCommand command, CancellationToken cancellationToken = default);

    ValueTask ITelegramEventHandler<Message>.OnEvent(ITelegramBotClient bot, Message eventPayload, CancellationToken cancellationToken)
    {
        var command = new TCommand();
        return command.InMessage(eventPayload) ? HandleAsync(bot, eventPayload.Chat, eventPayload.From, command, cancellationToken) : ValueTask.CompletedTask;
    }
}
internal interface IInlineCommandHandler<in TCommand>: ITelegramEventHandler<CallbackQuery> where TCommand : IInlineCommand, new() 
{
    ValueTask HandleAsync(ITelegramBotClient botClient,long chatId, TCommand command, CancellationToken cancellationToken = default);

    ValueTask ITelegramEventHandler<CallbackQuery>.OnEvent(ITelegramBotClient bot,  CallbackQuery eventPayload, CancellationToken cancellationToken)
    {
        var command = new TCommand();
        return command.TryFromCallbackQuery(eventPayload, out var chatId) ? 
            HandleAsync(bot, chatId, command, cancellationToken) : 
            ValueTask.CompletedTask;
    }
}