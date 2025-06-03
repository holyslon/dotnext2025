using Microsoft.Extensions.DependencyInjection.Extensions;
using NetworkingBot.Conversations;
using NetworkingBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace NetworkingBot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetworkingBot(this IServiceCollection services)
    {
        services.AddSingleton<IUpdateHandler, UpdateHandler>();
        services.AddSingleton<ConversationDb>();
        services.AddTelegramEventHandlers<Message>(opts =>
            opts.Add<StartHandler>()
                .Add<JoinHandler>()
                .Add<CancelHandler>()
                .Add<EventFinishedHandler>()
                .Add<MatchmakeHandler>());
        services.AddTelegramEventHandlers<CallbackQuery>(opts =>
            opts.Add<StartHandler>()
                .Add<EventFinishedHandler>());

        services.AddSingleton<IMatchmakingService, MatchmakingService>();
        return services;
    }

    private record CompositeEventHandlerConfig<T>(IReadOnlyList<Type> Handlers);

    private class CompositeEventHandler<T>(
        ILogger<CompositeEventHandler<T>> logger,
        IServiceProvider serviceProvider,
        CompositeEventHandlerConfig<T> config) : ITelegramEventHandler<T>
    {
        public async Task OnEvent(ITelegramBotClient bot, T eventPayload, CancellationToken cancellationToken)
        {
            foreach (var configHandler in config.Handlers)
            {
                using var _ = logger.BeginScope(new { HandlerType = configHandler.Name });
                if (serviceProvider.GetService(configHandler) is ITelegramEventHandler<T> handler)
                    await handler.OnEvent(bot, eventPayload, cancellationToken);
                else
                    logger.LogInformation("Fail to find handler in di");
            }
        }
    }

    internal class TypeListBuilder<TEvent>
    {
        private readonly List<Type> _items = [];

        internal TypeListBuilder<TEvent> Add<T>() where T : ITelegramEventHandler<TEvent>
        {
            _items.Add(typeof(T));
            return this;
        }

        internal IReadOnlyList<Type> Build()
        {
            return _items.AsReadOnly();
        }
    }

    internal static IServiceCollection AddTelegramEventHandlers<T>(this IServiceCollection services,
        Action<TypeListBuilder<T>> configure)
    {
        var builder = new TypeListBuilder<T>();
        configure(builder);

        var readOnlyList = builder.Build();
        services.AddSingleton(new CompositeEventHandlerConfig<T>(readOnlyList));
        foreach (var type in readOnlyList) services.TryAddScoped(type);
        services.AddScoped<ITelegramEventHandler<T>, CompositeEventHandler<T>>();


        return services;
    }
}