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
    services.AddCommonOutboxServices();

    services.AddSingleton(new LogSettings(assemblyFullNameWhereIntegrationEventsStore));
    services.AddTransient<IIntegrationEventLogPersistence, IntegrationEventLogService>();

    return services;
  }
}
