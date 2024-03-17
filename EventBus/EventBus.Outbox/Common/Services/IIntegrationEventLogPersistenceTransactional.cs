namespace Tsw.EventBus.Outbox.Common;

public interface IIntegrationEventLogPersistenceTransactional : IIntegrationEventLogPersistence
{
  /// <summary>
  /// Save event across a specific transaction.
  /// </summary>
  /// <param name="event">Event for saving.</param>
  /// <param name="currentTransaction">Specific transaction.</param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  Task SaveEventWithAsync(DbTransaction currentTransaction, params IntegrationEvent[] events);
}
