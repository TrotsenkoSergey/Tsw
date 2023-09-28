using System.Data.Common;

namespace Tsw.EventBus.Outbox.Services;

public interface IIntegrationEventLogService
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

  Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync();
  Task MarkEventAsPublishedAsync(Guid eventId);
  Task MarkEventAsInProgressAsync(Guid eventId);
  Task MarkEventAsFailedAsync(Guid eventId);
}
