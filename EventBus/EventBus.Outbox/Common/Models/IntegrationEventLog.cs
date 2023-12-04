namespace Tsw.EventBus.Outbox.Common;

/// <summary>
/// Container for Integration event, have to be Json - Content or (and) IntegartionEvent after cast.
/// </summary>
public class IntegrationEventLog
{
  public Guid Id { get; set; }

  public DateTime CreatedOnUtc { get; set; }

  public IntegrationEventState State { get; set; }

  public int TimesSent { get; set; }

  public string EventTypeName { get; set; } = default!;

  private string? _eventTypeShortName;

  public string EventTypeShortName
  {
    get => _eventTypeShortName ?? EventTypeName.Split('.')?.Last() ?? EventTypeName;
    set => _eventTypeShortName = value;
  }

  public string? Content { get; set; } // deserialized IntegrationEvent

  public IntegrationEvent? IntegrationEvent { get; set; } // IntegrationEvent after cast
}
