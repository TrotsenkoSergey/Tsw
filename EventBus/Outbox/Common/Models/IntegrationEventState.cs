namespace Tsw.EventBus.Outbox.Common;

public enum IntegrationEventState
{
  NotPublished = 1,
  InProgress = 2,
  Published = 3,
  PublishedFailed = 4
}
