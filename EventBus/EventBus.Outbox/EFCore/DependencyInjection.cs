namespace Tsw.EventBus.Outbox.EFCore;

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
    services.AddCommonOutboxServices();

    services.AddSingleton(new LogSettings(assemblyFullNameWhereIntegrationEventsStore));
    services.AddTransient<IIntegrationEventLogPersistenceTransactional, IntegrationEventLogService>();
    services.AddTransient<IIntegrationEventLogPersistence>(sp => sp.GetRequiredService<IIntegrationEventLogPersistenceTransactional>());

    return services;
  }


}
