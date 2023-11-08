namespace Tsw.EventBus.Outbox.Common;

public interface IIntegrationEventOutboxTransactional : IIntegrationEventOutboxService
{
  Task AddAndSaveEventAsync(IntegrationEvent evt, DbConnection dbConnection, DbTransaction currentTransaction);
}
