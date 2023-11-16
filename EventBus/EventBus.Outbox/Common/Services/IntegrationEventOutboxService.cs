namespace Tsw.EventBus.Outbox.Common;

public class IntegrationEventOutboxService : IIntegrationEventOutboxTransactional
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

  public virtual async Task AddAndSaveEventWithAsync(DbTransaction transaction, IntegrationEvent @event)
  {
    _logger.LogInformation("Enqueuing integration event to repository ({@IntegrationEvent})", @event);

    var eventService = _serviceProvider.GetRequiredService<IIntegrationEventLogPersistenceTransactional>();
    await eventService.SaveEventWithAsync(transaction, @event);

    await ActivateBackGroundTasksAsync();
  }

  public virtual async Task AddAndSaveEventAsync(IntegrationEvent @event)
  {
    _logger.LogInformation("Enqueuing integration event to repository ({@IntegrationEvent})", @event);

    var eventService = _serviceProvider.GetRequiredService<IIntegrationEventLogPersistence>();
    await eventService.SaveEventAsync(@event);

    await ActivateBackGroundTasksAsync();
  }

  protected virtual async Task ActivateBackGroundTasksAsync()
  {
    var factory = _serviceProvider.GetRequiredService<ISchedulerFactory>();
    var backgroundTasks = await factory.GetScheduler();
    await backgroundTasks.TriggerJob(new JobKey(nameof(ProcessOutboxMessagesJob)));
  }

  public virtual async Task GetAndPublishEventsThroughEventBusAsync(bool withoutAdditionalSerialization)
  {
    var eventService = _serviceProvider.GetRequiredService<IIntegrationEventLogPersistence>();

    if (withoutAdditionalSerialization)
    {
      var integrationEventsInJson = await eventService.GetEventLogsAwaitingToPublishInJsonAsync();
      await PublishIntegrationEvents(integrationEventsInJson, eventService);
      return;
    }

    var integrationEvents = await eventService.GetEventLogsAwaitingToPublishAsync();
    await PublishIntegrationEvents(integrationEvents, eventService);
  }

  protected virtual async Task PublishIntegrationEvents(
    IEnumerable<IntegrationEventLog> integrationEvents, IIntegrationEventLogPersistence eventService)
  {
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
        await eventService.MarkEventAsInProgressAsync(@event.Id);
        _eventBus.Publish(@event.IntegrationEvent!); // after GetEventLogsAwaitingToPublishAsync() it's not null
        await eventService.MarkEventAsPublishedAsync(@event.Id);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error publishing integration event: {IntegrationEventId}", @event.Id);

        await eventService.MarkEventAsFailedAsync(@event.Id);
      }
    }
  }

  protected virtual async Task PublishIntegrationEvents(
    IEnumerable<PublishContent> publishContents, IIntegrationEventLogPersistence eventService)
  {
    if (!publishContents.Any())
    {
      return;
    }

    _logger.LogInformation("Got {count} integration events.", publishContents.Count());

    foreach (var publishContent in publishContents)
    {
      _logger.LogInformation("Publishing integration event: ({@IntegrationEvent})", publishContent.JsonContent);

      try
      {
        await eventService.MarkEventAsInProgressAsync(publishContent.EventId);
        _eventBus.Publish(publishContent); // after GetEventLogsAwaitingToPublishAsync() it's not null
        await eventService.MarkEventAsPublishedAsync(publishContent.EventId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error publishing integration event: {IntegrationEventId}", publishContent.EventId);

        await eventService.MarkEventAsFailedAsync(publishContent.EventId);
      }
    }
  }
}
