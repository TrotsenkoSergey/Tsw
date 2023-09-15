﻿namespace Tsw.EventBus.Outbox;

public static class DependencyInjection
{
  /// <summary>
  /// Add services with default PostgreSql data provider and default migrations.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="connectionString">Same as your microservice</param>
  /// <param name="assemblyFullNameWhereIntegrationEventsStore"></param>
  /// <returns></returns>
  public static IServiceCollection AddOutboxIntegrationEvents(
    this IServiceCollection services,
    string connectionString,
    string assemblyFullNameWhereIntegrationEventsStore)
  {
    services.AddDbContext<IntegrationEventLogContext>(
      options => options.UseNpgsql(
        connectionString,
        opt => opt.EnableRetryOnFailure(
          maxRetryCount: 15,
          maxRetryDelay: TimeSpan.FromSeconds(30),
          errorCodesToAdd: null)
        ));

    services.AddIntegrationEventServices(assemblyFullNameWhereIntegrationEventsStore);

    return services;
  }

  /// <summary>
  /// Here you need to init migrations for IntegrationEventLogContext.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="optionsAction"></param>
  /// <param name="assemblyFullNameWhereIntegrationEventsStore"></param>
  /// <returns></returns>
  public static IServiceCollection AddOutboxIntegrationEvents(
    this IServiceCollection services,
    Action<DbContextOptionsBuilder> optionsAction,
    string assemblyFullNameWhereIntegrationEventsStore)
  {
    services.AddDbContext<IntegrationEventLogContext>(optionsAction);

    services.AddIntegrationEventServices(assemblyFullNameWhereIntegrationEventsStore);

    return services;
  }

  private static IServiceCollection AddIntegrationEventServices(
    this IServiceCollection services, string assemblyFullNameWhereIntegrationEventsStore)
  {
    services.AddTransient<IIntegrationEventLogService>(sp =>
    {
      var context = sp.GetRequiredService<IntegrationEventLogContext>();
      return new IntegrationEventLogService(assemblyFullNameWhereIntegrationEventsStore, context);
    });

    services.AddTransient<IIntegrationEventOutboxService, IntegrationEventOutboxService>();

    return services;
  }
}