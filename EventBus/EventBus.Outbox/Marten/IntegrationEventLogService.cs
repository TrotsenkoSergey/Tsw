namespace Tsw.EventBus.Outbox.Marten;

internal class IntegrationEventLogService : IIntegrationEventLogPersistenceTransactional
{
  protected readonly ILogger<IntegrationEventLogService> _logger;
  protected readonly IDocumentStore _store;

  public IntegrationEventLogService(ILogger<IntegrationEventLogService> logger, IDocumentStore store)
  {
    _logger = logger;
    _store = store;
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

    var eventLogEntry = await session.LoadAsync<IntegrationEventLog>(eventId);

    if (eventLogEntry is null)
    {
      throw new InvalidOperationException($"Cant find log entry with id - {eventId}");
    }

    eventLogEntry.State = status;

    if (status == IntegrationEventState.InProgress)
      eventLogEntry.TimesSent++;

    session.Update(eventLogEntry);

    await session.SaveChangesAsync();
  }

  public virtual Task SaveEventAsync(IntegrationEvent @event)
  {
    var eventLogEntry = CreateIntegrationEventLog(@event);

    return PersistEventLog(eventLogEntry);
  }

  public virtual Task SaveEventWithAsync(DbTransaction currentTransaction, IntegrationEvent @event)
  {
    var transaction = currentTransaction as NpgsqlTransaction;
    if (transaction is null)
    {
      throw new MartenException($"Transaction have to be {nameof(NpgsqlTransaction)}.");
    }

    var eventLogEntry = CreateIntegrationEventLog(@event);

    var sessionOptions = SessionOptions.ForTransaction(transaction);
    return PersistEventLog(eventLogEntry, sessionOptions);
  }

  protected virtual IntegrationEventLog CreateIntegrationEventLog(IntegrationEvent @event)
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

    return eventLogEntry;
  }

  protected virtual async Task PersistEventLog(IntegrationEventLog eventLog, SessionOptions? sessionOptions = default)
  {
    IDocumentSession session = default!;
    try
    {
      session = sessionOptions is not null
        ? await _store.LightweightSerializableSessionAsync(sessionOptions)
        : await _store.LightweightSerializableSessionAsync();

      session.Store(eventLog);
      await session.SaveChangesAsync();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Can not persist integration event {event}, because {exeption}.", eventLog, ex.Message);
    }
    finally
    {
      session?.Dispose();
    }
  }
}
