namespace Tsw.EventBus.Outbox.Services;

public interface IIntegrationEventOutboxService
{
  Task PublishEventsThroughEventBusAsync(Guid transactionId);
  Task AddAndSaveEventAsync(IntegrationEvent evt, IDbContextTransaction currentTransaction);
}
