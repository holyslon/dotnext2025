using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace NetworkingBot.Commands;

public interface IInlineCommand
{
    public string Text { get; }
    
    public string DataTag { get; }
    
    public InlineKeyboardButton Button(Chat chat) => InlineKeyboardButton.WithCallbackData(Text,$"{chat.Id}:{DataTag}");

    public bool TryFromCallbackQuery(CallbackQuery callbackQuery, out long chatId)
    {
        chatId = 0;
        var data = callbackQuery.Data;
        if (data == null)
        {
            return false;
        }
        var split = data.Split(':');
        if (split.Length != 2)
        {
            return false;
        }

        return DataTag.Equals(split[1], StringComparison.Ordinal) && 
               long.TryParse(split[0], out chatId);
    }
}

public static class IInlineCommandExtensions
{
    public static InlineKeyboardButton Button<TCommand>(this TCommand command, long chatId) where TCommand: IInlineCommand => InlineKeyboardButton.WithCallbackData(command.Text,$"{chatId}:{command.DataTag}");
}