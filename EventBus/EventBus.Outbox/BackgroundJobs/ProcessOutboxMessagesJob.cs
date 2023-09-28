using Quartz;

namespace Tsw.EventBus.Outbox.BackgroundJobs;

[DisallowConcurrentExecution]
internal class ProcessOutboxMessagesJob : IJob
{
  private readonly IIntegrationEventOutboxService _outboxService;

  public ProcessOutboxMessagesJob(IIntegrationEventOutboxService outboxService)
  {
    _outboxService = outboxService;
  }

  public Task Execute(IJobExecutionContext context) =>
    _outboxService.GetAndPublishEventsThroughEventBusAsync();
}
