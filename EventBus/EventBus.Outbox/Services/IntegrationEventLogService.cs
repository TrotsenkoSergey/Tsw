using System.Data.Common;

namespace Tsw.EventBus.Outbox.Services;

public class IntegrationEventLogService : IIntegrationEventLogService
{
  protected readonly IServiceProvider _sp;
  protected readonly List<Type> _eventTypes;

  public IntegrationEventLogService(
    OutboxSettings outboxSettings,
    IServiceProvider sp)
  {
    _sp = sp;
    _eventTypes = Assembly.Load(outboxSettings.AssemblyFullNameWhereIntegrationEventsStore)
        .GetTypes()
        .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
        .ToList();
  }

  public virtual async Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync(
    Guid transactionId)
  {
    string tid = transactionId.ToString();

    var context = _sp.GetRequiredService<IntegrationEventLogContext>();

    var result = await context.Set<IntegrationEventLog>()
        .Where(e => e.TransactionId == tid && e.State == EventStateEnum.NotPublished).ToListAsync();

    if (result.Any())
    {
      return result.OrderBy(o => o.CreatedOnUtc)
          .Select(e => e.DeserializeJsonContent(_eventTypes.Find(t => t.Name == e.EventTypeShortName)!));
    }

    return new List<IntegrationEventLog>();
  }

  public virtual Task SaveEventAsync(
    IntegrationEvent @event, Transaction transaction, DbConnection dbConnection)
  {
    if (transaction == null) throw new ArgumentNullException(nameof(transaction));
    if (dbConnection == null) throw new ArgumentNullException(nameof(dbConnection));

    var context = _sp.GetRequiredService<IntegrationEventLogContext>();
    context.Database.SetDbConnection(dbConnection);
    context.Database.UseTransaction(transaction.Current);

    var eventLogEntry = new IntegrationEventLog(@event, transaction.Id);
    context.Set<IntegrationEventLog>().Add(eventLogEntry);

    return context.SaveChangesAsync();
  }

  public virtual Task MarkEventAsPublishedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, EventStateEnum.Published);

  public virtual Task MarkEventAsInProgressAsync(Guid eventId) =>
    UpdateEventStatus(eventId, EventStateEnum.InProgress);

  public virtual Task MarkEventAsFailedAsync(Guid eventId) =>
    UpdateEventStatus(eventId, EventStateEnum.PublishedFailed);

  protected virtual Task UpdateEventStatus(Guid eventId, EventStateEnum status)
  {
    var context = _sp.GetRequiredService<IntegrationEventLogContext>();

    var eventLogEntry = context.Set<IntegrationEventLog>().Single(e => e.EventId == eventId);
    eventLogEntry.State = status;

    if (status == EventStateEnum.InProgress)
      eventLogEntry.TimesSent++;

    context.Set<IntegrationEventLog>().Update(eventLogEntry);

    return context.SaveChangesAsync();
  }
}
