namespace Tsw.EventBus.RabbitMQ;

public static class DependencyInjection
{
     public static IServiceCollection AddRabbitMQEventBus(
         this IServiceCollection services, IConfiguration configuration)
    {
        RabbitMQConfuguration rabbitConf = new();
        configuration.GetSection(nameof(RabbitMQConfuguration)).Bind(rabbitConf);
        services.Configure<RabbitMQConfuguration>((conf) =>
        {
            conf.HostName = rabbitConf.HostName;
            conf.Port = rabbitConf.Port;
            conf.UserName = rabbitConf.UserName;
            conf.Password = rabbitConf.Password;
            conf.RetryCount = rabbitConf.RetryCount;
            conf.ClientProvidedName = rabbitConf.ClientProvidedName;
            conf.ExchangeName = rabbitConf.ExchangeName;
            conf.QueueName = rabbitConf.QueueName;
        });

        services.AddSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();
            var conf = sp.GetRequiredService<IOptionsMonitor<RabbitMQConfuguration>>().CurrentValue;

            var factory = new ConnectionFactory()
            {
                DispatchConsumersAsync = true
            };

            if (!string.IsNullOrEmpty(conf.HostName))
            {
                factory.HostName = conf.HostName;
            }

            if (!conf.Port.Equals(default))
            {
                factory.Port = conf.Port;
            }

            if (!string.IsNullOrEmpty(conf.UserName))
            {
                factory.UserName = conf.UserName;
            }

            if (!string.IsNullOrEmpty(conf.Password))
            {
                factory.Password = conf.Password;
            }

            if (!string.IsNullOrEmpty(conf.ClientProvidedName))
            {
                factory.ClientProvidedName = conf.ClientProvidedName;
            }

            if (!conf.RetryCount.Equals(default))
            {
                return new DefaultRabbitMQPersistentConnection(factory, logger, conf.RetryCount);
            }

            return new DefaultRabbitMQPersistentConnection(factory, logger);
        });

        services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

        services.AddSingleton<IEventBus, RabbitMQEventBus>(serviceProvider =>
        {
            var conf = serviceProvider
                .GetRequiredService<IOptionsMonitor<RabbitMQConfuguration>>().CurrentValue;
            var rabbitMQPersistentConnection = serviceProvider
                .GetRequiredService<IRabbitMQPersistentConnection>();
            var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQEventBus>>();
            var eventBusSubcriptionsManager = serviceProvider
                .GetRequiredService<IEventBusSubscriptionsManager>();

            if (!conf.RetryCount.Equals(default))
            {
                return new RabbitMQEventBus(logger, rabbitMQPersistentConnection,
                    serviceProvider, eventBusSubcriptionsManager,
                    conf.ExchangeName, conf.QueueName, conf.RetryCount);
            }

            return new RabbitMQEventBus(logger, rabbitMQPersistentConnection,
                serviceProvider, eventBusSubcriptionsManager, conf.ExchangeName, conf.QueueName);
        });

        return services;
    }
}
