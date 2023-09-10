namespace Tsw.EventBus.Outbox;

public record Transaction(IDbContextTransaction Current);
