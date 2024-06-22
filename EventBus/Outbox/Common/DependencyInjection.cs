namespace Tsw.EventBus.Outbox.Common;

public static class DependencyInjection
{
  public static IServiceCollection AddCommonOutboxServices(this IServiceCollection services) 
  {
    services.AddScoped<IIntegrationEventOutboxTransactional, IntegrationEventOutboxService>();
    services.AddScoped<IIntegrationEventOutboxService>(sp => sp.GetRequiredService<IIntegrationEventOutboxTransactional>());

    services.AddQuartzJobs();

    return services;
  }

  private static IServiceCollection AddQuartzJobs(this IServiceCollection services)
  {
    services.AddQuartz(configure =>
    {
      var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));

      configure
        .AddJob<ProcessOutboxMessagesJob>(jobKey, o => o.StoreDurably())
        .AddTrigger(trigger => trigger.ForJob(jobKey));
    });

    services.AddQuartzHostedService();

    return services;
  }
}
