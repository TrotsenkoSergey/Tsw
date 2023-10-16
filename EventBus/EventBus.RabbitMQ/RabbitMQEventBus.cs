using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Tsw.EventBus.RabbitMQ;

public class RabbitMQEventBus : IEventBus, IDisposable
{
  private const int DEFAULT_RETRY_COUNT = 5;
  private readonly int _retryCount;

  private readonly ILogger<RabbitMQEventBus> _logger;
  private readonly IRabbitMQPersistentConnection _persistentConnection;
  private readonly IEventBusSubscriptionsManager _subsManager;
  private readonly IServiceProvider _serviceProvider;

  private IModel _consumerChannel;
  private readonly string _exchangeName;
  private string _queueName;

  private readonly JsonSerializerOptions _jsonOptions;

  public RabbitMQEventBus(
      ILogger<RabbitMQEventBus> logger,
      IRabbitMQPersistentConnection persistentConnection,
      IServiceProvider serviceProvider,
      IEventBusSubscriptionsManager subsManager,
      string exchangeName,
      string queueName,
      int retryCount = DEFAULT_RETRY_COUNT)
  {
    ArgumentNullException.ThrowIfNull(logger);
    ArgumentNullException.ThrowIfNull(persistentConnection);
    ArgumentNullException.ThrowIfNull(serviceProvider);
    _logger = logger;
    _persistentConnection = persistentConnection;
    _serviceProvider = serviceProvider;
    _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
    _exchangeName = exchangeName;
    _queueName = queueName;
    _retryCount = retryCount;
    _consumerChannel = CreateConsumerChannel();
    _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    _jsonOptions = CreateJsonOptions();
  }

  private void SubsManager_OnEventRemoved(object? sender, string eventName)
  {
    if (!_persistentConnection.IsConnected)
    {
      _persistentConnection.TryConnect();
    }

    using var channel = _persistentConnection.CreateModel();
    channel.QueueUnbind(queue: _queueName,
        exchange: _exchangeName,
        routingKey: eventName);

    if (_subsManager.IsEmpty)
    {
      _queueName = string.Empty;
      _consumerChannel.Close();
    }
  }

  public void Publish(IntegrationEvent @event)
  {
    if (!_persistentConnection.IsConnected)
    {
      _persistentConnection.TryConnect();
    }

    var policy = RetryPolicy.Handle<BrokerUnreachableException>()
        .Or<SocketException>()
        .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
        {
          _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
        });

    var eventName = @event.GetType().Name;

    _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

    using var channel = _persistentConnection.CreateModel();
    _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

    channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct);

    var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), _jsonOptions);

    policy.Execute(() =>
    {
      var properties = channel.CreateBasicProperties();
      properties.DeliveryMode = 2; // persistent

      _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);

      channel.BasicPublish(
              exchange: _exchangeName,
              routingKey: eventName,
              mandatory: true,
              basicProperties: properties,
              body: body);
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

      _consumerChannel.QueueBind(queue: _queueName,
                          exchange: _exchangeName,
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
          queue: _queueName,
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
    var eventType = _subsManager.GetEventTypeByName(eventName);

    var objectFromBody = JsonSerializer.Deserialize(e.Body.Span, eventType, _jsonOptions);
    string jsonMessage = JsonSerializer.Serialize(objectFromBody, eventType, _jsonOptions);

    try
    {
      if (jsonMessage.ToLowerInvariant().Contains("throw-fake-exception"))
      {
        throw new InvalidOperationException($"Fake exception requested: \"{jsonMessage}\"");
      }

      await ProcessEvent(eventName, jsonMessage);
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

    channel.ExchangeDeclare(exchange: _exchangeName,
                            type: ExchangeType.Direct);

    channel.QueueDeclare(queue: _queueName,
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

  private async Task ProcessEvent(string eventName, string message)
  {
    _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

    if (_subsManager.HasSubscriptionsForEvent(eventName))
    {
      using IServiceScope scope = _serviceProvider.CreateScope();
      var subscriptions = _subsManager.GetHandlersForEvent(eventName);
      foreach (var subscription in subscriptions)
      {
        object handler = scope.ServiceProvider.GetRequiredService(subscription.HandlerType);
        if (subscription.IsDynamic)
        {
          if (handler is not IDynamicIntegrationEventHandler dynamicHandler) continue;
          using dynamic eventData = JsonDocument.Parse(message);
          await Task.Yield();
          await dynamicHandler.Handle(eventData);
        }
        else
        {
          if (handler == null) continue;
          var eventType = _subsManager.GetEventTypeByName(eventName);
          object? integrationEvent = JsonSerializer.Deserialize(message, eventType, _jsonOptions);

          Type concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

          await Task.Yield();
          await (Task)concreteType
              .GetMethod("Handle")
              .Invoke(handler, new object[] { integrationEvent });
        }
      }
    }
    else
    {
      _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
    }
  }

  private JsonSerializerOptions CreateJsonOptions() =>
    new()
    {
      WriteIndented = true,
      Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
      PropertyNameCaseInsensitive = true
    };
}
