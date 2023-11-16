using System.Text.Json.Serialization;

namespace Tsw.EventBus.Outbox.Common;

public class IntegrationEventLog
{
  private readonly JsonSerializerOptions _indentedOptions;
  private readonly JsonSerializerOptions _caseInsensitiveOptions;

  public IntegrationEventLog(IntegrationEvent @event) : this()
  {
    Id = @event.Id;
    Type eventType = @event.GetType();
    Content = JsonSerializer.Serialize(@event, eventType, _indentedOptions);
    CreatedOnUtc = @event.CreationDate;
    State = IntegrationEventState.NotPublished;
    TimesSent = 0;
    EventTypeName = eventType.FullName!;
  }

  [JsonConstructor]
  public IntegrationEventLog(Guid id, IntegrationEventState state, string content,
    int timesSent, DateTime createdOnUtc, string eventTypeName) : this()
  {
    Id =id;
    State = state;
    Content = content;
    TimesSent = timesSent;
    CreatedOnUtc = createdOnUtc;
    EventTypeName = eventTypeName;
  }

  public Guid Id { get; private set; }

  public string Content { get; private set; }

  public DateTime CreatedOnUtc { get; private set; }

  public IntegrationEventState State { get; set; }

  public int TimesSent { get; set; }

  public string EventTypeName { get; private set; }

  [JsonIgnore]
  public string EventTypeShortName => EventTypeName.Split('.')?.Last() ?? EventTypeName;

  private IntegrationEvent? _integrationEvent;
  [JsonIgnore]
  public IntegrationEvent? IntegrationEvent
  {
    get
    {
      if (_integrationEvent == null) 
      {
        var eventType = Type.GetType(EventTypeName)!;
        _integrationEvent = DeserializeJsonContent(eventType).IntegrationEvent;
      }

      return _integrationEvent;
    }
  }

  public IntegrationEventLog DeserializeJsonContent(Type type)
  {
    _integrationEvent = JsonSerializer.Deserialize(Content, type, _caseInsensitiveOptions) as IntegrationEvent;

    if (_integrationEvent is null)
    {
      throw new JsonException($"Can't deserialize {type.FullName} integration event.");
    }

    return this;
  }

#pragma warning disable CS8618
  private IntegrationEventLog()
  {
    var encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic);
    _indentedOptions = new JsonSerializerOptions() { Encoder = encoder, WriteIndented = true };
    _caseInsensitiveOptions = new JsonSerializerOptions() { Encoder = encoder, PropertyNameCaseInsensitive = true };
  }
#pragma warning restore CS8618
}
