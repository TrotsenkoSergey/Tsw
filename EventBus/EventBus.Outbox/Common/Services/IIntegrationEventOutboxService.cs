namespace Tsw.EventBus.Outbox.Common;

public interface IIntegrationEventOutboxService
{
  Task AddAndSaveEventAsync(IntegrationEvent @event);
  Task GetAndPublishEventsThroughEventBusAsync(bool inJson = false);
}
