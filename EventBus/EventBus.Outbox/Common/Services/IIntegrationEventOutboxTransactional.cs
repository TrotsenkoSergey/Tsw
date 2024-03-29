﻿namespace Tsw.EventBus.Outbox.Common;

public interface IIntegrationEventOutboxTransactional : IIntegrationEventOutboxService
{
  Task AddAndSaveEventWithAsync(DbTransaction currentTransaction, params IntegrationEvent[] events);
}
