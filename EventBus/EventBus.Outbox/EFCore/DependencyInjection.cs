namespace Tsw.EventBus.Outbox;

public static class DependencyInjection
{
  /// <summary>
  /// Add outbox integration events services with DbContext and EventBus.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="optionsAction"></param>
  /// <param name="assemblyFullNameWhereIntegrationEventsStore"></param>
  /// <returns></returns>
  /// <remarks>By default Npgsql provider.</remarks>
  public static IServiceCollection AddOutboxIntegrationEvents(this IServiceCollection services,
    string connectionString, string assemblyFullNameWhereIntegrationEventsStore)
  {
    services.AddDbContext<IntegrationEventLogDbContext>(opt =>
      opt.UseNpgsql(connectionString, opt =>
      {
        opt.MigrationsAssembly(typeof(IntegrationEventLogDbContext).Assembly.FullName);
        opt.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "eventslog");
      }), ServiceLifetime.Transient);

    services.AddOutboxServices(assemblyFullNameWhereIntegrationEventsStore);

    return services;
  }

  /// <summary>
  /// Add outbox integration events services with DbContext and EventBus.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="optionsBuilder"></param>
  /// <param name="assemblyFullNameWhereIntegrationEventsStore"></param>
  /// <returns></returns>
  /// <remarks>You need to specify options builder for DbContext and create migration.</remarks>
  public static IServiceCollection AddOutboxIntegrationEvents(
    this IServiceCollection services,
    Action<DbContextOptionsBuilder> optionsBuilder,
    string assemblyFullNameWhereIntegrationEventsStore)
  {
    services.AddDbContext<IntegrationEventLogDbContext>(optionsBuilder, ServiceLifetime.Transient);
    services.AddOutboxServices(assemblyFullNameWhereIntegrationEventsStore);
    return services;
  }

  private static IServiceCollection AddOutboxServices(this IServiceCollection services, string assemblyFullNameWhereIntegrationEventsStore) 
  {
    services.AddSingleton(new OutboxSettings(assemblyFullNameWhereIntegrationEventsStore));

    services.AddTransient<IIntegrationEventLogPersistenceTransactional, IntegrationEventLogService>();
    services.AddTransient<IIntegrationEventLogPersistence>(sp => sp.GetRequiredService<IIntegrationEventLogPersistenceTransactional>());

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
