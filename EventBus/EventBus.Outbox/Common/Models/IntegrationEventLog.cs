namespace Tsw.EventBus.Outbox.Common;

public class IntegrationEventLog
{
  private readonly JavaScriptEncoder _encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic);
  private readonly JsonSerializerOptions _indentedOptions;
  private readonly JsonSerializerOptions _caseInsensitiveOptions;

  public IntegrationEventLog(IntegrationEvent @event) : this()
  {
    EventId = @event.Id;
    Type eventType = @event.GetType();
    Content = JsonSerializer.Serialize(@event, eventType, _indentedOptions);
    CreatedOnUtc = @event.CreationDate;
    State = IntegrationEventState.NotPublished;
    TimesSent = 0;
    EventTypeName = eventType.FullName!;
  }

  public Guid EventId { get; private set; }

  public string Content { get; private set; }

  public DateTime CreatedOnUtc { get; private set; }

  public IntegrationEventState State { get; set; }

  public int TimesSent { get; set; }

  public string EventTypeName { get; private set; }

  public string EventTypeShortName => EventTypeName.Split('.')?.Last() ?? EventTypeName;

  public IntegrationEvent? IntegrationEvent { get; private set; }

  public IntegrationEventLog DeserializeJsonContent(Type type)
  {
    IntegrationEvent = JsonSerializer.Deserialize(Content, type, _caseInsensitiveOptions) as IntegrationEvent;

    if (IntegrationEvent is null)
    {
      throw new JsonException($"Can't deserialize {type.FullName} integration event.");
    }

    return this;
  }

#pragma warning disable CS8618
  private IntegrationEventLog()
  {
    _indentedOptions = new JsonSerializerOptions() { Encoder = _encoder, WriteIndented = true };
    _caseInsensitiveOptions = new JsonSerializerOptions() { Encoder = _encoder, PropertyNameCaseInsensitive = true };
  }
#pragma warning restore CS8618
}
