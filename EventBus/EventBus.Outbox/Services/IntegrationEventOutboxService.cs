namespace Tsw.EventBus.Outbox.Services;

public class IntegrationEventOutboxService : IIntegrationEventOutboxService
{
  protected readonly ILogger<IntegrationEventOutboxService> _logger;
  protected readonly IEventBus _eventBus;
  protected readonly IIntegrationEventLogService _eventLogService;
  protected bool _needPublish;

  public IntegrationEventOutboxService(
    ILogger<IntegrationEventOutboxService> logger,
    IEventBus eventBus,
    IIntegrationEventLogService integrationEventLogServiceFactory)
  {
    _eventBus = eventBus;
    _eventLogService = integrationEventLogServiceFactory;
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

    await _eventLogService.SaveEventAsync(@event, transaction);
    _needPublish = true;
  }

  public virtual async Task PublishEventsThroughEventBusAsync(Guid transactionId)
  {
    var integrationEvents =
      await _eventLogService.GetEventLogsAwaitingToPublishAsync(transactionId);

    foreach (var @event in integrationEvents)
    {
      _logger.LogInformation("Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.EventId, @event.IntegrationEvent);

      try
      {
        await _eventLogService.MarkEventAsInProgressAsync(@event.EventId);
        _eventBus.Publish(@event.IntegrationEvent!); // after GetEventLogsAwaitingToPublishAsync() it's not null
        await _eventLogService.MarkEventAsPublishedAsync(@event.EventId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error publishing integration event: {IntegrationEventId}", @event.EventId);

        await _eventLogService.MarkEventAsFailedAsync(@event.EventId);
      }
    }
  }
}
