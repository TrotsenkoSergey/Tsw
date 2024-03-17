namespace Tsw.EventBus.Outbox.Common;

public interface IIntegrationEventLogPersistence
{
  Task SaveEventAsync(params IntegrationEvent[] events);
  Task<IEnumerable<PublishContent>> GetEventLogsAwaitingToPublishInJsonAsync();
  Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync();
  Task MarkEventAsPublishedAsync(Guid eventId);
  Task MarkEventAsInProgressAsync(Guid eventId);
  Task MarkEventAsFailedAsync(Guid eventId);
}
