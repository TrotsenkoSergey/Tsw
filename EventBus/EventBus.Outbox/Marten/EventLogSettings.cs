namespace Tsw.EventBus.Outbox.Marten;

public record EventLogSettings(
  string AssemblyFullNameWhereIntegrationEventsStore
);
