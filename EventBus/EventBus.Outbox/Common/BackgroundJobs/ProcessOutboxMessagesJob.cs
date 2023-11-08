namespace Tsw.EventBus.Outbox.Common;

[DisallowConcurrentExecution]
public class ProcessOutboxMessagesJob : IJob
{
  private readonly IIntegrationEventOutboxTransactional _outboxService;

  public ProcessOutboxMessagesJob(IIntegrationEventOutboxTransactional outboxService)
  {
    _outboxService = outboxService;
  }

  public Task Execute(IJobExecutionContext context) =>
    _outboxService.GetAndPublishEventsThroughEventBusAsync();
}
