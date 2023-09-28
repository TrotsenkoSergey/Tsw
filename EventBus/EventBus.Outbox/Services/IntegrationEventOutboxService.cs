using System.Data.Common;

namespace Tsw.EventBus.Outbox.Services;

public class IntegrationEventOutboxService : IIntegrationEventOutboxService
{
  protected readonly ILogger<IntegrationEventOutboxService> _logger;
  protected readonly IEventBus _eventBus;
  protected readonly IServiceProvider _serviceProvider;

  public IntegrationEventOutboxService(
    ILogger<IntegrationEventOutboxService> logger,
    IEventBus eventBus,
    IServiceProvider serviceProvider)
  {
    _eventBus = eventBus;
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  public virtual async Task AddAndSaveEventAsync(
    IntegrationEvent @event, DbConnection dbConnection, DbTransaction transaction)
  {
    _logger.LogInformation("Enqueuing integration event to repository ({@IntegrationEvent})", @event);

    var eventService = _serviceProvider.GetRequiredService<IIntegrationEventLogService>();
    await eventService.SaveEventAsync(@event, dbConnection, transaction);
  }

  public virtual async Task GetAndPublishEventsThroughEventBusAsync()
  {
    var eventService = _serviceProvider.GetRequiredService<IIntegrationEventLogService>();
    var integrationEvents = await eventService.GetEventLogsAwaitingToPublishAsync();

    if (!integrationEvents.Any())
    {
      return;
    }

    _logger.LogInformation("Got {count} integration events.", integrationEvents.Count());

    foreach (var @event in integrationEvents)
    {
      _logger.LogInformation("Publishing integration event: ({@IntegrationEvent})", @event.IntegrationEvent);

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
