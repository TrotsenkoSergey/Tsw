using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Tsw.EventBus.Outbox;

public class IntegrationEventLog
{
  private readonly JsonSerializerOptions _indentedOptions = new() 
  { 
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic), 
    WriteIndented = true 
  };

  private readonly JsonSerializerOptions _caseInsensitiveOptions = new() 
  {
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
    PropertyNameCaseInsensitive = true 
  };

  public IntegrationEventLog(IntegrationEvent @event, Guid transactionId)
  {
    EventId = @event.Id;
    Content = JsonSerializer.Serialize(@event, @event.GetType(), _indentedOptions);
    CreatedOnUtc = @event.CreationDate;
    State = EventStateEnum.NotPublished;
    TimesSent = 0;
    EventTypeName = @event.GetType().FullName!;
    TransactionId = transactionId.ToString();
  }

  public Guid EventId { get; private set; }

  public string Content { get; private set; }

  public DateTime CreatedOnUtc { get; private set; }

  public EventStateEnum State { get; set; }

  public int TimesSent { get; set; }

  public string EventTypeName { get; private set; }

  public string TransactionId { get; private set; }

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

  /// <summary>
  /// Initializes a new instance of the <see cref="IntegrationEventLog"/> class.
  /// </summary>
  /// <remarks>
  /// Required by EF Core.
  /// </remarks>
#pragma warning disable CS8618
  private IntegrationEventLog() { }
#pragma warning restore CS8618
}
