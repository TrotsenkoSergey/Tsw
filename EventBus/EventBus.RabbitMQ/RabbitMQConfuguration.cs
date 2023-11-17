namespace Tsw.EventBus.RabbitMQ;

public class RabbitMQConfuguration
{
  public const string SectionName = nameof(RabbitMQConfuguration);
  public string? HostName { get; set; } // by default "localhost"
  public int Port { get; set; } // by default 5672
  public string? UserName { get; set; } // by default "guest"
  public string? Password { get; set; } // by default "guest"
  public int RetryCount { get; set; } // by default 5
  public string? ClientProvidedName { get; set; }
  public string ExchangeName { get; set; } = "unnamed_exchange";
  public string QueueName { get; set; } = "unnamed_queue";
}
