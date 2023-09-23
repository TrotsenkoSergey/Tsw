using Microsoft.EntityFrameworkCore.Migrations;

namespace Tsw.EventBus.Outbox;

public static class DependencyInjection
{
  /// <summary>
  /// Here you need to init migrations for IntegrationEventLogContext.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="optionsAction"></param>
  /// <param name="assemblyFullNameWhereIntegrationEventsStore"></param>
  /// <returns></returns>
  public static IServiceCollection AddOutboxIntegrationEvents<MainDbContext>(
    this IServiceCollection services,
    string assemblyFullNameWhereIntegrationEventsStore,
  string connectionString)
    where MainDbContext : DbContext
  {
    services.AddPooledDbContextFactory<IntegrationEventLogContext>(opt =>
      opt.UseNpgsql(connectionString, opt =>
      {
        //opt.EnableRetryOnFailure(
        //  maxRetryCount: 15,
        //  maxRetryDelay: TimeSpan.FromSeconds(30),
        //  errorCodesToAdd: null);
        opt.MigrationsAssembly(typeof(IntegrationEventLogContext).Assembly.FullName);
        opt.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "eventslog");
      }));
    services.AddSingleton<IntegrationEventLogContextScopedFactory<MainDbContext>>();
    services.AddScoped(sp => sp.GetRequiredService<IntegrationEventLogContextScopedFactory<MainDbContext>>().CreateDbContext());


    services.AddScoped<IIntegrationEventLogService>(sp =>
    {
      var context = sp.GetRequiredService<IntegrationEventLogContextScopedFactory<MainDbContext>>();
      return new IntegrationEventLogService(assemblyFullNameWhereIntegrationEventsStore, context);
    });

    services.AddScoped<IIntegrationEventOutboxService, IntegrationEventOutboxService>();

    return services;
  }
}
