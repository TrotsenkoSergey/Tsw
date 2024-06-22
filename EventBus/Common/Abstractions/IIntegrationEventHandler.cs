namespace Tsw.EventBus.Common.Abstractions;

public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
  Task Handle(TIntegrationEvent @event);
}

public interface IIntegrationEventHandler
{
}
