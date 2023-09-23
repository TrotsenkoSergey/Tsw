namespace Tsw.EventBus.Outbox.Services;

public class IntegrationEventOutboxService : IIntegrationEventOutboxService
{
  protected readonly ILogger<IntegrationEventOutboxService> _logger;
  protected readonly IEventBus _eventBus;
  private readonly IServiceProvider _serviceProvider;
  protected bool _needPublish;

  public IntegrationEventOutboxService(
    ILogger<IntegrationEventOutboxService> logger,
    IEventBus eventBus,
    IServiceProvider serviceProvider)
  {
    _eventBus = eventBus;
    _serviceProvider = serviceProvider;
    _logger = logger;
    _needPublish = false;
  }

  /// <summary>
  /// Flag created after AddAndSaveEventAsync() method call. Not actual in different scope lifetimes.
  /// </summary>
  public virtual bool NeedPublish => _needPublish;

  public virtual async Task AddAndSaveEventAsync(
    IntegrationEvent @event, Transaction transaction)
  {
    _logger.LogInformation(
      "Enqueuing integration event {IntegrationEventId} to repository ({@IntegrationEvent})", @event.Id, @event);

    var eventService = _serviceProvider.GetRequiredService<IIntegrationEventLogService>();
    await eventService.SaveEventAsync(@event, transaction);

    _needPublish = true;
  }

  public virtual async Task PublishEventsThroughEventBusAsync(Guid transactionId)
  {
    var eventService = _serviceProvider.GetRequiredService<IIntegrationEventLogService>();
    var integrationEvents =
      await eventService.GetEventLogsAwaitingToPublishAsync(transactionId);

    foreach (var @event in integrationEvents)
    {
      _logger.LogInformation("Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.EventId, @event.IntegrationEvent);

      try
      {
        await eventService.MarkEventAsInProgressAsync(@event.EventId);
        _eventBus.Publish(@event.IntegrationEvent!); // after GetEventLogsAwaitingToPublishAsync() it's not null
        await eventService.MarkEventAsPublishedAsync(@event.EventId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error publishing integration event: {IntegrationEventId}", @event.EventId);

        await eventService.MarkEventAsFailedAsync(@event.EventId);
      }
    }
  }
}
