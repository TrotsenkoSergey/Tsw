namespace Tsw.EventBus.Outbox.Common;

public class IntegrationEventLog
{
  public Guid Id { get; set; }

  public string? Content { get; set; }

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

  public IntegrationEvent? IntegrationEvent { get; set; }
}
