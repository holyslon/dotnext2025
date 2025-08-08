using Microsoft.Extensions.DependencyInjection.Extensions;
using NetworkingBot.Handlers;
using NetworkingBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace NetworkingBot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetworkingBot(this IServiceCollection services)
    {
        services.AddSingleton<IUpdateHandler, UpdateHandler>();
        services.AddTelegramEventHandlers<Message>(opts =>
            opts.Add<StartCommandHandler>()
                .Add<JoinCommandHandler>()
                .Add<OnlineCommandHandler>()
                .Add<OfflineCommandHandler>()
                .Add<MeetingHappenCommandHandler>()
                .Add<MeetingCanceledCommandHandler>()
                .Add<PostponeCommandHandler>());
        services.AddTelegramEventHandlers<CallbackQuery>(opts =>
            opts.Add<JoinCommandHandler>()
                .Add<OnlineCommandHandler>()
                .Add<OfflineCommandHandler>()
                .Add<ReadyForMeetingCommand>()
                .Add<MeetingHappenCommandHandler>()
                .Add<MeetingCanceledCommandHandler>()
                .Add<PostponeCommandHandler>());
        services.AddTelegramEventHandlers<Poll>(opts =>
            opts.Add<ConversationTopicPoolResponse>());

        services.AddSingleton<IUserStorage, UserStorage>();
        services.AddSingleton<IConversationTopicStorage, ConversationTopicStorage>();
        services.AddSingleton<IPollStorage, PollStorage>();
        services.AddSingleton<IMatchService, MatchService>();

        return services;
    }

    private record CompositeEventHandlerConfig<T>(IReadOnlyList<Type> Handlers);

    private class CompositeEventHandler<T>(
        ILogger<CompositeEventHandler<T>> logger,
        IServiceProvider serviceProvider,
        CompositeEventHandlerConfig<T> config) : ITelegramEventHandler<T>
    {
        public async ValueTask OnEvent(ITelegramBotClient bot, T eventPayload, CancellationToken cancellationToken)
        {
            foreach (var configHandler in config.Handlers)
            {
                using var _ = logger.BeginScope(new { HandlerType = configHandler.Name });
                try
                {
                    if (serviceProvider.GetService(configHandler) is ITelegramEventHandler<T> handler)
                        await handler.OnEvent(bot, eventPayload, cancellationToken);
                    else
                        logger.LogInformation("Fail to find handler in di");
                }
                catch (ObjectDisposedException e)
                {
                    logger.LogInformation(e, "Service collection disposed");
                    break;
                }

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