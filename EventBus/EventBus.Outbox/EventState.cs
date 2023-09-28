namespace Tsw.EventBus.Outbox;

public enum EventState
{
  NotPublished = 1,
  InProgress = 2,
  Published = 3,
  PublishedFailed = 4
}
