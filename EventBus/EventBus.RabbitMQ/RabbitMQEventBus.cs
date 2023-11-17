namespace Tsw.EventBus.RabbitMQ;

public class RabbitMQEventBus : IEventBus, IDisposable
{
  private const int DEFAULT_RETRY_COUNT = 5;
  private readonly int _retryCount;
  private readonly ILogger<RabbitMQEventBus> _logger;
  private readonly IRabbitMQPersistentConnection _persistentConnection;
  private readonly IEventBusSubscriptionsManager _subsManager;
  private readonly RabbitMQConfuguration _rabbitOptions;
  private readonly JsonSerializerOptions _jsonOptions;
  private readonly IServiceScopeFactory _serviceScopeFactory;
  private IModel _consumerChannel;

  public RabbitMQEventBus(
      ILogger<RabbitMQEventBus> logger,
      IRabbitMQPersistentConnection persistentConnection,
      IServiceScopeFactory serviceScopeFactory,
      IEventBusSubscriptionsManager subsManager,
      IOptionsMonitor<JsonSerializerOptions> jsonOptions,
      IOptionsMonitor<RabbitMQConfuguration> rabbitOptions)
  {
    ArgumentNullException.ThrowIfNull(logger);
    ArgumentNullException.ThrowIfNull(persistentConnection);
    ArgumentNullException.ThrowIfNull(serviceScopeFactory);
    ArgumentNullException.ThrowIfNull(subsManager);
    ArgumentNullException.ThrowIfNull(jsonOptions);
    ArgumentNullException.ThrowIfNull(rabbitOptions);

    _logger = logger;
    _persistentConnection = persistentConnection;
    _serviceScopeFactory = serviceScopeFactory;
    _subsManager = subsManager;
    _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    _jsonOptions = jsonOptions.CurrentValue;
    _rabbitOptions = rabbitOptions.CurrentValue;
    _retryCount = _rabbitOptions.RetryCount == default
      ? DEFAULT_RETRY_COUNT
      : _rabbitOptions.RetryCount;
    _consumerChannel = CreateConsumerChannel();
  }

  private void SubsManager_OnEventRemoved(object? sender, string eventName)
  {
    if (!_persistentConnection.IsConnected)
    {
      _persistentConnection.TryConnect();
    }

    using var channel = _persistentConnection.CreateModel();
    channel.QueueUnbind(queue: _rabbitOptions.QueueName,
        exchange: _rabbitOptions.ExchangeName,
        routingKey: eventName);

    if (_subsManager.IsEmpty)
    {
      _rabbitOptions.QueueName = string.Empty;
      _consumerChannel.Close();
    }
  }

  public void Publish(IntegrationEvent @event)
  {
    var eventType = @event.GetType();
    var eventTypeName = eventType.Name;
    var content = JsonSerializer.SerializeToUtf8Bytes(@event, eventType, _jsonOptions);
    Publish(@event.Id, eventTypeName, content);
  }

  public void Publish(PublishContent publishContent)
  {
    var content = Encoding.UTF8.GetBytes(publishContent.JsonContent);
    Publish(publishContent.EventId, publishContent.IntegrationEventTypeName, content);
  }

  private void Publish(Guid eventId, string eventTypeName, byte[] content)
  {
    if (!_persistentConnection.IsConnected)
    {
      _persistentConnection.TryConnect();
    }

    int retryCount = _rabbitOptions.RetryCount == default ? DEFAULT_RETRY_COUNT : _rabbitOptions.RetryCount;

    var policy = RetryPolicy
        .Handle<BrokerUnreachableException>()
        .Or<SocketException>()
        .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
        {
          _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", eventId, $"{time.TotalSeconds:n1}", ex.Message);
        });

    _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", eventId, eventTypeName);
    using var channel = _persistentConnection.CreateModel();

    _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", eventId);
    channel.ExchangeDeclare(exchange: _rabbitOptions.ExchangeName, type: ExchangeType.Direct);

    policy.Execute(() =>
    {
      var properties = channel.CreateBasicProperties();
      properties.DeliveryMode = 2; // persistent

      _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", eventId);

      channel.BasicPublish(
              exchange: _rabbitOptions.ExchangeName,
              routingKey: eventTypeName,
              mandatory: true,
              basicProperties: properties,
              body: content);
    });
  }

  public void SubscribeDynamic<TH>(string eventName)
      where TH : IDynamicIntegrationEventHandler
  {
    _logger.LogInformation("Subscribing to dynamic event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

    DoInternalSubscription(eventName);
    _subsManager.AddDynamicSubscription<TH>(eventName);
    StartBasicConsume();
  }

  public void Subscribe<T, TH>()
      where T : IntegrationEvent
      where TH : IIntegrationEventHandler<T>
  {
    var eventName = _subsManager.GetEventKey<T>();
    DoInternalSubscription(eventName);

    _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

    _subsManager.AddSubscription<T, TH>();
    StartBasicConsume();
  }

  private void DoInternalSubscription(string eventName)
  {
    var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
    if (!containsKey)
    {
      if (!_persistentConnection.IsConnected)
      {
        _persistentConnection.TryConnect();
      }

      _consumerChannel.QueueBind(queue: _rabbitOptions.QueueName,
                          exchange: _rabbitOptions.ExchangeName,
                          routingKey: eventName);
    }
  }

  public void Unsubscribe<T, TH>()
      where T : IntegrationEvent
      where TH : IIntegrationEventHandler<T>
  {
    var eventName = _subsManager.GetEventKey<T>();

    _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

    _subsManager.RemoveSubscription<T, TH>();
  }

  public void UnsubscribeDynamic<TH>(string eventName)
      where TH : IDynamicIntegrationEventHandler
  {
    _subsManager.RemoveDynamicSubscription<TH>(eventName);
  }

  public void Dispose()
  {
    if (_consumerChannel != null)
    {
      _consumerChannel.Dispose();
    }

    _subsManager.Clear();
  }

  private void StartBasicConsume()
  {
    _logger.LogTrace("Starting RabbitMQ basic consume");

    if (_consumerChannel != null)
    {
      var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

      consumer.Received += Consumer_Received;

      _consumerChannel.BasicConsume(
          queue: _rabbitOptions.QueueName,
          autoAck: false,
          consumer: consumer);
    }
    else
    {
      _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
    }
  }

  private async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
  {
    string eventName = e.RoutingKey;
    string jsonMessage = Encoding.UTF8.GetString(e.Body.Span);

    try
    {
      if (jsonMessage.ToLowerInvariant().Contains("throw-fake-exception"))
      {
        throw new InvalidOperationException($"Fake exception requested: \"{jsonMessage}\"");
      }

      await ProcessEvent(eventName, e.Body);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", jsonMessage);
    }

    // Even on exception we take the message off the queue.
    // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
    // For more information see: https://www.rabbitmq.com/dlx.html
    _consumerChannel.BasicAck(e.DeliveryTag, multiple: false);
  }

  private IModel CreateConsumerChannel()
  {
    if (!_persistentConnection.IsConnected)
    {
      _persistentConnection.TryConnect();
    }

    _logger.LogTrace("Creating RabbitMQ consumer channel");

    var channel = _persistentConnection.CreateModel();

    channel.ExchangeDeclare(exchange: _rabbitOptions.ExchangeName,
                            type: ExchangeType.Direct);

    channel.QueueDeclare(queue: _rabbitOptions.QueueName,
                         durable: true,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    channel.CallbackException += (sender, ea) =>
    {
      _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

      _consumerChannel.Dispose();
      _consumerChannel = CreateConsumerChannel();
      StartBasicConsume();
    };

    return channel;
  }

  private async Task ProcessEvent(string eventName, ReadOnlyMemory<byte> body)
  {
    _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

    if (_subsManager.HasSubscriptionsForEvent(eventName))
    {
      using IServiceScope scope = _serviceScopeFactory.CreateScope();
      var subscriptions = _subsManager.GetHandlersForEvent(eventName);
      foreach (var subscription in subscriptions)
      {
        object? handler = scope.ServiceProvider.GetService(subscription.HandlerType);

        if (subscription.IsDynamic)
        {
          if (handler is not IDynamicIntegrationEventHandler dynamicHandler) continue;
          using dynamic eventData = JsonDocument.Parse(body);
          await Task.Yield();
          await dynamicHandler.Handle(eventData);
          continue;
        }

        if (handler == null)
        {
          continue;
        }

        var eventType = _subsManager.GetEventTypeByName(eventName);
        object? integrationEvent = JsonSerializer.Deserialize(body.Span, eventType, _jsonOptions);

        Type concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

        await Task.Yield();
        await (Task)concreteType.GetMethod("Handle")!.Invoke(handler, new object[] { integrationEvent });
      }
    }
    else
    {
      _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
    }
  }
}
