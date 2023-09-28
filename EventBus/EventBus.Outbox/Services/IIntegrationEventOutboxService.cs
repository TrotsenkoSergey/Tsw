using System.Data.Common;

namespace Tsw.EventBus.Outbox.Services;

public interface IIntegrationEventOutboxService
{
  Task AddAndSaveEventAsync(IntegrationEvent evt, DbConnection dbConnection, DbTransaction currentTransaction);
  Task GetAndPublishEventsThroughEventBusAsync();
}
