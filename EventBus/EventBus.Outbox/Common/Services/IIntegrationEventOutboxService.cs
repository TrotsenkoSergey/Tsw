namespace Tsw.EventBus.Outbox.Common;

public interface IIntegrationEventOutboxService
{
  Task AddAndSaveEventAsync(params IntegrationEvent[] events);
  Task GetAndPublishEventsThroughEventBusAsync(bool withoutAdditionalSerialization);
}
