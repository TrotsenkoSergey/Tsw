namespace Tsw.EventBus.Outbox.Common;

[DisallowConcurrentExecution]
public class ProcessOutboxMessagesJob : IJob
{
  private const bool DefaultInJson = true;
  private readonly IIntegrationEventOutboxService _outboxService;

  public ProcessOutboxMessagesJob(
    IIntegrationEventOutboxService outboxService)
  {
    _outboxService = outboxService;
  }

  public async Task Execute(IJobExecutionContext context)
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    
    var data = context.MergedJobDataMap;
    bool inJson = DefaultInJson;
    data.TryGetBoolean("inJson", out inJson);
    
    await _outboxService.GetAndPublishEventsThroughEventBusAsync(inJson);
  }
}
