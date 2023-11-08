namespace Tsw.EventBus.Outbox.Marten;

internal class IntegrationEventLogService : IIntegrationEventLogPersistence
{
  protected readonly List<Type> _eventTypes;
  protected readonly IDocumentStore _store;

  public IntegrationEventLogService(IDocumentStore store, OutboxSettings outboxSettings)
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
    using var session = _store.QuerySession();

    return await session.Query<IntegrationEventLog>()
        .Where(e => e.State == IntegrationEventState.NotPublished)
        .Select(e => e.DeserializeJsonContent(_eventTypes.Find(t => t.Name == e.EventTypeShortName)!))
        .ToListAsync();
  }

  public virtual async Task<IEnumerable<PublishContent>> GetEventLogsAwaitingToPublishInJsonAsync()
  {
    using var session = _store.QuerySession();

    var logs = await session.Query<IntegrationEventLog>()
      .Where(e => e.State == IntegrationEventState.NotPublished)
      .ToListAsync();

    return logs.Select(log => new PublishContent(log.EventId, log.Content, log.EventTypeName));
  }

  public virtual Task MarkEventAsPublishedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, IntegrationEventState.Published);

  public virtual Task MarkEventAsInProgressAsync(Guid eventId) =>
    UpdateEventStatus(eventId, IntegrationEventState.InProgress);

  public virtual Task MarkEventAsFailedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, IntegrationEventState.PublishedFailed);

  protected virtual async Task UpdateEventStatus(Guid eventId, IntegrationEventState status)
  {
    using var session = await _store.LightweightSerializableSessionAsync();

    var eventLogEntry = await session.Query<IntegrationEventLog>().FirstAsync(e => e.EventId == eventId);
    eventLogEntry.State = status;

    if (status == IntegrationEventState.InProgress)
      eventLogEntry.TimesSent++;

    session.Update(eventLogEntry);

    await session.SaveChangesAsync();
  }

  public virtual async Task SaveEventAsync(IntegrationEvent @event)
  {
    using var session = await _store.LightweightSerializableSessionAsync();

    var eventLogEntry = new IntegrationEventLog(@event);
    session.Store(eventLogEntry);

    await session.SaveChangesAsync();
  }
}
