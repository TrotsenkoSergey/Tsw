﻿namespace Tsw.EventBus.Common.Abstractions;

public interface IEventBus
{
  void Publish(IntegrationEvent @event);

  void Publish(PublishContent publishContent);

  void Subscribe<T, TH>(bool autoStartBasicConsume = true)
      where T : IntegrationEvent
      where TH : IIntegrationEventHandler<T>;

  void SubscribeDynamic<TH>(string eventName)
      where TH : IDynamicIntegrationEventHandler;

  void UnsubscribeDynamic<TH>(string eventName)
      where TH : IDynamicIntegrationEventHandler;

  void Unsubscribe<T, TH>()
      where TH : IIntegrationEventHandler<T>
      where T : IntegrationEvent;
}
