﻿namespace Tsw.EventBus.Outbox.EFCore;

public class IntegrationEventLogService : IIntegrationEventLogPersistenceTransactional
{
  protected readonly IReadOnlyList<Type> _eventTypes;
  protected readonly IntegrationEventLogDbContext _context;
  private readonly JsonSerializerOptions _jsonSerializerOptions;

  public IntegrationEventLogService(
    EventLogSettings outboxSettings,
    IntegrationEventLogDbContext context,
    IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptions)
  {
    _eventTypes = outboxSettings.EventTypes;
    _context = context;
    _jsonSerializerOptions = jsonSerializerOptions.Get("Outbox.EFCore");
  }

  public virtual async Task<IEnumerable<IntegrationEventLog>> GetEventLogsAwaitingToPublishAsync()
  {
    var result = await GetNotPublishedEvents();

    if (!result.Any())
    {
      return Enumerable.Empty<IntegrationEventLog>();
    }

    return result.OrderBy(e => e.CreatedOnUtc)
        .Select(e => DeserializeJsonContent(e, _eventTypes.First(t => t.Name == e.EventTypeShortName)));
  }

  public virtual async Task<IEnumerable<PublishContent>> GetEventLogsAwaitingToPublishInJsonAsync()
  {
    var result = await GetNotPublishedEvents();

    if (!result.Any())
    {
      return Enumerable.Empty<PublishContent>();

    }

    return result.OrderBy(e => e.CreatedOnUtc)
        .Select(e => new PublishContent(e.Id, e.Content!, e.EventTypeShortName));
  }

  protected virtual Task<List<IntegrationEventLog>> GetNotPublishedEvents() =>
    _context.Set<IntegrationEventLog>()
      .Where(e => e.State == IntegrationEventState.NotPublished)
      .ToListAsync();

  public virtual Task SaveEventWithAsync(DbTransaction currentTransaction, params IntegrationEvent[] events)
  {
    ArgumentNullException.ThrowIfNull(nameof(currentTransaction));

    _context.Database.SetDbConnection(currentTransaction.Connection);
    _context.Database.UseTransaction(currentTransaction);

    return SaveEventAsync(events);
  }

  public virtual Task SaveEventAsync(params IntegrationEvent[] events)
  {
    List<IntegrationEventLog> integrationEventLogs = new();

    foreach (var @event in events)
    {
      Type eventType = @event.GetType();
      var eventLogEntry = new IntegrationEventLog()
      {
        Id = @event.Id,
        IntegrationEvent = @event,
        Content = SerializeJsonContent(@event, eventType),
        CreatedOnUtc = @event.CreationDate,
        State = IntegrationEventState.NotPublished,
        TimesSent = 0,
        EventTypeName = eventType.FullName!
      };
      integrationEventLogs.Add(eventLogEntry);
    }

    _context.Set<IntegrationEventLog>().AddRange(integrationEventLogs);

    return _context.SaveChangesAsync();
  }

  public virtual Task MarkEventAsPublishedAsync(Guid eventId) =>
    UpdateEventStatusAsync(eventId, IntegrationEventState.Published);

  public virtual Task MarkEventAsInProgressAsync(Guid eventId) =>
    UpdateEventStatusAsync(eventId, IntegrationEventState.InProgress);

  public virtual Task MarkEventAsFailedAsync(Guid eventId) =>
    UpdateEventStatusAsync(eventId, IntegrationEventState.PublishedFailed);

  protected virtual Task UpdateEventStatusAsync(Guid eventId, IntegrationEventState status)
  {
    var eventLogEntry = _context.Set<IntegrationEventLog>()
      .Single(e => e.Id == eventId);

    eventLogEntry.State = status;
    if (eventLogEntry.State == IntegrationEventState.InProgress)
    {
      eventLogEntry.TimesSent++;
    }

    return _context.SaveChangesAsync();
  }

  private IntegrationEventLog DeserializeJsonContent(IntegrationEventLog eventLog, Type type)
  {
    if (eventLog.Content is null)
    {
      throw new NullReferenceException(nameof(eventLog.Content));
    }

    eventLog.IntegrationEvent =
      JsonSerializer.Deserialize(eventLog.Content, type, _jsonSerializerOptions) as IntegrationEvent ??
        throw new JsonException($"Can't deserialize {type.FullName} integration event.");

    return eventLog;
  }

  private string SerializeJsonContent(IntegrationEvent @event, Type type) =>
    JsonSerializer.Serialize(@event, type, _jsonSerializerOptions);
}
