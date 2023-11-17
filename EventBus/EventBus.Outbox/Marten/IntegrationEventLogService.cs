﻿namespace Tsw.EventBus.Outbox.Marten;

internal class IntegrationEventLogService : IIntegrationEventLogPersistence
{
  protected readonly List<Type> _eventTypes;
  protected readonly IDocumentStore _store;

  public IntegrationEventLogService(IDocumentStore store, EventLogSettings outboxSettings)
  {
    _store = store;
    _eventTypes = Assembly
        .Load(outboxSettings.AssemblyFullNameWhereIntegrationEventsStore)
        .GetTypes()
        .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
        .ToList();
  }

  public virtual async Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync()
  {
    using var session = await _store.QuerySerializableSessionAsync();

    var result = await session.Query<IntegrationEventLog>()
      .Where(e => e.State == IntegrationEventState.NotPublished)
      .ToListAsync();

    return result;
  }

  public virtual async Task<IEnumerable<PublishContent>> GetEventLogsAwaitingToPublishInJsonAsync()
  {
    var result = await GetEventLogsAwaitingToPublishAsync();
    return result.Select(log => new PublishContent(log.Id, log.Content!, log.EventTypeShortName));
  }

  public virtual Task MarkEventAsPublishedAsync(Guid eventId) =>
      UpdateEventStatusAsync(eventId, IntegrationEventState.Published);

  public virtual Task MarkEventAsInProgressAsync(Guid eventId) =>
    UpdateEventStatusAsync(eventId, IntegrationEventState.InProgress);

  public virtual Task MarkEventAsFailedAsync(Guid eventId) =>
    UpdateEventStatusAsync(eventId, IntegrationEventState.PublishedFailed);

  protected virtual async Task UpdateEventStatusAsync(Guid eventId, IntegrationEventState status)
  {
    using var session = await _store.LightweightSerializableSessionAsync();

    var eventLogEntry = await session.Query<IntegrationEventLog>().FirstAsync(e => e.Id == eventId);
    eventLogEntry.State = status;

    if (status == IntegrationEventState.InProgress)
      eventLogEntry.TimesSent++;

    session.Update(eventLogEntry);

    await session.SaveChangesAsync();
  }

  public virtual async Task SaveEventAsync(IntegrationEvent @event)
  {
    Type eventType = @event.GetType();
    var eventLogEntry = new IntegrationEventLog()
    {
      Id = @event.Id,
      CreatedOnUtc = @event.CreationDate,
      IntegrationEvent = @event,
      State = IntegrationEventState.NotPublished,
      TimesSent = 0,
      EventTypeName = eventType.FullName!,
    };

    using var session = await _store.LightweightSerializableSessionAsync();
    session.Store(eventLogEntry);

    await session.SaveChangesAsync();
  }
}
