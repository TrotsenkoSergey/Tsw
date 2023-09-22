namespace Tsw.EventBus.Outbox.Services;

public class IntegrationEventLogService<ApplicationDbContext> : IIntegrationEventLogService
  where ApplicationDbContext : DbContext
{
  protected readonly IntegrationEventLogContext<ApplicationDbContext> _integrationEventLogContext;
  protected readonly List<Type> _eventTypes;

  public IntegrationEventLogService(
    string assemblyFullNameWhereIntegrationEventsStore,
    IntegrationEventLogContext<ApplicationDbContext> context)
  {
    _integrationEventLogContext = context;
    _eventTypes = Assembly.Load(assemblyFullNameWhereIntegrationEventsStore)
        .GetTypes()
        .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
        .ToList();
  }

  public virtual async Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync(
    Guid transactionId)
  {
    string tid = transactionId.ToString();

    var result = await _integrationEventLogContext.Set<IntegrationEventLog>()
        .Where(e => e.TransactionId == tid && e.State == EventStateEnum.NotPublished).ToListAsync();

    if (result.Any())
    {
      return result.OrderBy(o => o.CreatedOnUtc)
          .Select(e => e.DeserializeJsonContent(_eventTypes.Find(t => t.Name == e.EventTypeShortName)!));
    }

    return new List<IntegrationEventLog>();
  }

  public virtual Task SaveEventAsync(IntegrationEvent @event, Transaction transaction)
  {
    if (transaction == null) throw new ArgumentNullException(nameof(transaction));

    _integrationEventLogContext.Database.UseTransaction(transaction.Current);

    var eventLogEntry = new IntegrationEventLog(@event, transaction.Id);
    _integrationEventLogContext.Set<IntegrationEventLog>().Add(eventLogEntry);

    return _integrationEventLogContext.SaveChangesAsync();
  }

  public virtual Task MarkEventAsPublishedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, EventStateEnum.Published);

  public virtual Task MarkEventAsInProgressAsync(Guid eventId) =>
    UpdateEventStatus(eventId, EventStateEnum.InProgress);

  public virtual Task MarkEventAsFailedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, EventStateEnum.PublishedFailed);

  protected virtual Task UpdateEventStatus(Guid eventId, EventStateEnum status)
  {
    var eventLogEntry = _integrationEventLogContext.Set<IntegrationEventLog>().Single(e => e.EventId == eventId);
    eventLogEntry.State = status;

    if (status == EventStateEnum.InProgress)
      eventLogEntry.TimesSent++;

    _integrationEventLogContext.Set<IntegrationEventLog>().Update(eventLogEntry);

    return _integrationEventLogContext.SaveChangesAsync();
  }
}
