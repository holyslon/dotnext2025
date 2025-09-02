using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace NetworkBotTest;

public class UpdateHandlerAndBot(IUpdateHandler updateHandler, BotMock botMock)
{
    public BotMock Mock { get; } = botMock;

    public async Task HandleUpdateAsync(Update update)
    {
        await updateHandler.HandleUpdateAsync(Mock, update, CancellationToken.None);
    }
}