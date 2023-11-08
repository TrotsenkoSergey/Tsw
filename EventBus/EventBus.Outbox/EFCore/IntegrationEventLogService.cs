namespace Tsw.EventBus.Outbox.EFCore;

public class IntegrationEventLogService : IIntegrationEventLogPersistenceTransactional
{
  protected readonly List<Type> _eventTypes;
  protected readonly IntegrationEventLogDbContext _context;

  public IntegrationEventLogService(
    OutboxSettings outboxSettings,
    IntegrationEventLogDbContext context)
  {
    _eventTypes = Assembly
        .Load(outboxSettings.AssemblyFullNameWhereIntegrationEventsStore)
        .GetTypes()
        .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
        .ToList();

    _context = context;
  }

  public virtual async Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync()
  {
    var result = await GetNotPublishedEvents();

    if (!result.Any())
    {
      return Enumerable.Empty<IntegrationEventLog>();
    }

    return result.OrderBy(e => e.CreatedOnUtc)
        .Select(e => e.DeserializeJsonContent(_eventTypes.Find(t => t.Name == e.EventTypeShortName)!));
  }

  public async Task<IEnumerable<PublishContent>> GetEventLogsAwaitingToPublishInJsonAsync()
  {
    var result = await GetNotPublishedEvents();

    if (!result.Any())
    {
      return Enumerable.Empty<PublishContent>();
    }

    return result.OrderBy(e => e.CreatedOnUtc)
        .Select(e => new PublishContent(e.EventId, e.Content, e.EventTypeName));
  }

  private Task<List<IntegrationEventLog>> GetNotPublishedEvents() =>
    _context.Set<IntegrationEventLog>().Where(e => e.State == IntegrationEventState.NotPublished).ToListAsync();

  public virtual Task SaveEventAsync(
    IntegrationEvent @event, DbConnection dbConnection, DbTransaction currentTransaction)
  {
    if (currentTransaction == null) throw new ArgumentNullException(nameof(currentTransaction));
    if (dbConnection == null) throw new ArgumentNullException(nameof(dbConnection));

    _context.Database.SetDbConnection(dbConnection);
    _context.Database.UseTransaction(currentTransaction);

    return SaveEventAsync(@event);
  }

  public Task SaveEventAsync(IntegrationEvent @event)
  {
    var eventLogEntry = new IntegrationEventLog(@event);
    _context.Set<IntegrationEventLog>().Add(eventLogEntry);

    return _context.SaveChangesAsync();
  }

  public virtual Task MarkEventAsPublishedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, IntegrationEventState.Published);

  public virtual Task MarkEventAsInProgressAsync(Guid eventId) =>
    UpdateEventStatus(eventId, IntegrationEventState.InProgress);

  public virtual Task MarkEventAsFailedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, IntegrationEventState.PublishedFailed);

  protected virtual Task UpdateEventStatus(Guid eventId, IntegrationEventState status)
  {
    var eventLogEntry = _context.Set<IntegrationEventLog>().Single(e => e.EventId == eventId);
    eventLogEntry.State = status;

    if (status == IntegrationEventState.InProgress)
      eventLogEntry.TimesSent++;

    _context.Set<IntegrationEventLog>().Update(eventLogEntry);

    return _context.SaveChangesAsync();
  }
}
