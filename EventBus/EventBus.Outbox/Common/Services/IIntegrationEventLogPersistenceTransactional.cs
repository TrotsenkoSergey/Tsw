namespace Tsw.EventBus.Outbox.Common;

public interface IIntegrationEventLogPersistenceTransactional : IIntegrationEventLogPersistence
{
  /// <summary>
  /// Save event across a specific connection and transaction.
  /// </summary>
  /// <param name="event">Event for saving.</param>
  /// <param name="dbConnection">Specific connection.</param>
  /// <param name="currentTransaction">Specific transaction.</param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  Task SaveEventAsync(IntegrationEvent @event, DbConnection dbConnection, DbTransaction currentTransaction);

}
