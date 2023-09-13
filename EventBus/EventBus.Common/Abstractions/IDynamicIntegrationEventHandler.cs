namespace Tsw.EventBus.Common.Abstractions;

public interface IDynamicIntegrationEventHandler
{
  Task Handle(dynamic eventData);
}
