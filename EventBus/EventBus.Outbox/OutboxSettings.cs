namespace Tsw.EventBus.Outbox;

public record OutboxSettings(
  string AssemblyFullNameWhereIntegrationEventsStore
);
