namespace Tsw.EventBus.Outbox.Marten;

public record OutboxSettings(
  string AssemblyFullNameWhereIntegrationEventsStore
);
