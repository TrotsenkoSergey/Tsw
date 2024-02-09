namespace Tsw.EventBus.Common.Abstractions;

public interface IEventBus
{
  void Publish(IntegrationEvent @event);

  void Publish(PublishContent publishContent);

  void Subscribe<TEvent, THandler>(bool autoStartBasicConsume = true)
      where TEvent : IntegrationEvent
      where THandler : IIntegrationEventHandler<TEvent>;

  void SubscribeDynamic<THandler>(string eventName)
      where THandler : IDynamicIntegrationEventHandler;

  void UnsubscribeDynamic<THandler>(string eventName)
      where THandler : IDynamicIntegrationEventHandler;

  void Unsubscribe<TEvent, THandler>()
      where THandler : IIntegrationEventHandler<TEvent>
      where TEvent : IntegrationEvent;
}
