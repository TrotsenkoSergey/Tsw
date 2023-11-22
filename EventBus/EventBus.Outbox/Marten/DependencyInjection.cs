namespace Tsw.EventBus.Outbox.Marten;

public static class DependencyInjection
{
  public static IServiceCollection AddOutboxIntegrationEventsWithMarten(
    this IServiceCollection services, string connectionString)
  {
    services.AddOutboxServices();

    if (!services.Any(d => d.ServiceType == typeof(IDocumentStore)))
    {
      services.AddMarten(options =>
      {
        options.Connection(connectionString);

        options.UseDefaultSerialization(EnumStorage.AsString, Casing.SnakeCase);

        options.AutoCreateSchemaObjects = AutoCreate.All;
        options.DatabaseSchemaName = "public";
        options.Events.DatabaseSchemaName = "public";

        options.RegisterDocumentType<IntegrationEventLog>();
      });
    }

    return services;
  }

  private static IServiceCollection AddOutboxServices(this IServiceCollection services)
  {
    services.AddCommonOutboxServices();
    services.AddTransient<IIntegrationEventLogPersistence, IntegrationEventLogService>();

    return services;
  }
}
