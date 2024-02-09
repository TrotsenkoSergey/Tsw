namespace Tsw.EventBus.RabbitMQ;

public static class DependencyInjection
{
  public static IServiceCollection AddRabbitMQEventBus(
      this IServiceCollection services, IConfiguration configuration)
  {
    var options = configuration
      .GetRequiredSection(RabbitMQConfuguration.SectionName)
      .Get<RabbitMQConfuguration>()!;
    services.Configure<RabbitMQConfuguration>((conf) =>
    {
      conf.HostName = options.HostName;
      conf.Port = options.Port;
      conf.UserName = options.UserName;
      conf.Password = options.Password;
      conf.RetryCount = options.RetryCount;
      conf.ClientProvidedName = options.ClientProvidedName;
      conf.ExchangeName = options.ExchangeName;
      conf.QueueName = options.QueueName;
    });

    services.Configure<JsonSerializerOptions>((opt) =>
    {
      opt.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
      opt.Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic);
      opt.PropertyNameCaseInsensitive = true;
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
    services.AddSingleton<IEventBus, RabbitMQEventBus>();

    return services;
  }
}
