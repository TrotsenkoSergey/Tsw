using System.Data.Common;

namespace Tsw.EventBus.Outbox.Services;

public interface IIntegrationEventOutboxService
{
  bool NeedPublish { get; }
  Task PublishEventsThroughEventBusAsync(Guid transactionId);
  Task AddAndSaveEventAsync(IntegrationEvent evt, Transaction currentTransaction, DbConnection dbConnection);
}
