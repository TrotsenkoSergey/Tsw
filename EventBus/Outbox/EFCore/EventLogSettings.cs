namespace Tsw.EventBus.Outbox;

public record EventLogSettings(
  string AssemblyFullNameWhereIntegrationEventsStore,
  IReadOnlyList<Type> EventTypes
);
