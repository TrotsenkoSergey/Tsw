using System.Data.Common;

namespace Tsw.EventBus.Outbox.Services;

public class IntegrationEventLogService : IIntegrationEventLogService
{
  protected readonly List<Type> _eventTypes;
  protected readonly IntegrationEventLogContext _context;

  public IntegrationEventLogService(
    OutboxSettings outboxSettings,
    IntegrationEventLogContext context)
  {
    _eventTypes = Assembly.Load(outboxSettings.AssemblyFullNameWhereIntegrationEventsStore)
        .GetTypes()
        .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
        .ToList();
    _context = context;
  }

  public virtual async Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync()
  {
    var result = await _context.Set<IntegrationEventLog>()
        .Where(e => e.State == EventState.NotPublished)
        .ToListAsync();

    if (!result.Any())
    {
      return Enumerable.Empty<IntegrationEventLog>();
    }

    return result.OrderBy(o => o.CreatedOnUtc)
        .Select(e => e.DeserializeJsonContent(_eventTypes.Find(t => t.Name == e.EventTypeShortName)!));
  }

  public virtual Task SaveEventAsync(
    IntegrationEvent @event, DbConnection dbConnection, DbTransaction currentTransaction)
  {
    if (currentTransaction == null) throw new ArgumentNullException(nameof(currentTransaction));
    if (dbConnection == null) throw new ArgumentNullException(nameof(dbConnection));

    _context.Database.SetDbConnection(dbConnection);
    _context.Database.UseTransaction(currentTransaction);

    var eventLogEntry = new IntegrationEventLog(@event);
    _context.Set<IntegrationEventLog>().Add(eventLogEntry);

    return _context.SaveChangesAsync();
  }

  public virtual Task MarkEventAsPublishedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, EventState.Published);

  public virtual Task MarkEventAsInProgressAsync(Guid eventId) =>
    UpdateEventStatus(eventId, EventState.InProgress);

  public virtual Task MarkEventAsFailedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, EventState.PublishedFailed);

  protected virtual Task UpdateEventStatus(Guid eventId, EventState status)
  {
    var eventLogEntry = _context.Set<IntegrationEventLog>().Single(e => e.EventId == eventId);
    eventLogEntry.State = status;

    if (status == EventState.InProgress)
      eventLogEntry.TimesSent++;

    _context.Set<IntegrationEventLog>().Update(eventLogEntry);

    return _context.SaveChangesAsync();
  }
}
