using Microsoft.Extensions.DependencyInjection;

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
  public static IServiceCollection AddOutboxIntegrationEvents<ApplicationDbContext>(
    this IServiceCollection services,
    string assemblyFullNameWhereIntegrationEventsStore)
    where ApplicationDbContext : DbContext
  {
    
    services.AddDbContext<IntegrationEventLogContext<ApplicationDbContext>>();

    services.AddScoped<IIntegrationEventLogService>(sp =>
    {
      var context = sp.GetRequiredService<IntegrationEventLogContext<ApplicationDbContext>>();
      return new IntegrationEventLogService<ApplicationDbContext>(
        assemblyFullNameWhereIntegrationEventsStore, context);
    });

    services.AddScoped<IIntegrationEventOutboxService, IntegrationEventOutboxService>();

    return services;
  }
}
