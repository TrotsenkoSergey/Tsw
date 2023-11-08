namespace Tsw.EventBus.Outbox.Marten;

public static class DependencyInjection
{
  public static IServiceCollection AddOutboxIntegrationEventsWithMarten(
    this IServiceCollection services, string connectionString, string assemblyFullNameWhereIntegrationEventsStore)
  {

    if (!services.Any(d => d.ServiceType == typeof(IDocumentStore)))
    {
      services.AddMarten(options =>
      {
        options.Connection(connectionString);
        
        options.UseDefaultSerialization(EnumStorage.AsString, Casing.SnakeCase);

        options.AutoCreateSchemaObjects = AutoCreate.All;
        options.DatabaseSchemaName = "public";
        //options.Events.DatabaseSchemaName = "public";

        //options.Projections.Snapshot<IssuesList>(SnapshotLifecycle.Inline);

        options.RegisterDocumentType<IntegrationEventLog>();
      });
    }

    services.AddOutboxServices(assemblyFullNameWhereIntegrationEventsStore);

    return services;
  }

  private static IServiceCollection AddOutboxServices(this IServiceCollection services, string assemblyFullNameWhereIntegrationEventsStore)
  {
    services.AddSingleton(new OutboxSettings(assemblyFullNameWhereIntegrationEventsStore));

    services.AddTransient<IIntegrationEventLogPersistence, IntegrationEventLogService>();
    services.AddScoped<IIntegrationEventOutboxService, IntegrationEventOutboxService>();

    services.AddQuartzJobs();

    return services;
  }

  private static IServiceCollection AddQuartzJobs(this IServiceCollection services)
  {
    services.AddQuartz(configure =>
    {
      var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));

      configure
        .AddJob<ProcessOutboxMessagesJob>(jobKey)
        .AddTrigger(trigger =>
          trigger.ForJob(jobKey)
            .WithSimpleSchedule(schedule =>
              schedule.WithIntervalInSeconds(2)
                  .RepeatForever()));
    });

    services.AddQuartzHostedService();

    return services;
  }
}
