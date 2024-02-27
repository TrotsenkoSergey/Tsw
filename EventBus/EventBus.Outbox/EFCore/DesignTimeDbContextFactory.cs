namespace Tsw.EventBus.Outbox;

/// <summary>
/// Only for creation migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IntegrationEventLogDbContext>
{
  public IntegrationEventLogDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<IntegrationEventLogDbContext>();
    optionsBuilder.UseNpgsql(".", options =>
      {
        options.MigrationsAssembly(GetType().Assembly.GetName().Name);
        options.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "eventslog");
      });

    return new IntegrationEventLogDbContext(optionsBuilder.Options);
  }
}
