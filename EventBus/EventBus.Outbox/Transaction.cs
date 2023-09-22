using System.Data.Common;

namespace Tsw.EventBus.Outbox;

public record Transaction(Guid Id, DbTransaction Current);
