namespace Tsw.EventBus.Outbox.Services;

public interface IIntegrationEventLogService
{
  Task SaveEventAsync(IntegrationEvent @event, Transaction currentTransaction);
  Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync(Guid transactionId);
  Task MarkEventAsPublishedAsync(Guid eventId);
  Task MarkEventAsInProgressAsync(Guid eventId);
  Task MarkEventAsFailedAsync(Guid eventId);
}
