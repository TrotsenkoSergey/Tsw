namespace Tsw.EventBus.Outbox.Common;

[DisallowConcurrentExecution]
public class ProcessOutboxMessagesJob : IJob
{
  private readonly IIntegrationEventOutboxService _outboxService;

  public ProcessOutboxMessagesJob(IIntegrationEventOutboxService outboxService)
  {
    _outboxService = outboxService;
  }

  public async Task Execute(IJobExecutionContext context)
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    await _outboxService.GetAndPublishEventsThroughEventBusAsync();
  }
}
