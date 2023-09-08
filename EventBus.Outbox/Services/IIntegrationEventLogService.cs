namespace Tsw.EventBus.Outbox.Services;

public interface IIntegrationEventLogService
{
  Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync(Guid transactionId);
  Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction currentTransaction);
  Task MarkEventAsPublishedAsync(Guid eventId);
  Task MarkEventAsInProgressAsync(Guid eventId);
  Task MarkEventAsFailedAsync(Guid eventId);
}
