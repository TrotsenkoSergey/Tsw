using Microsoft.EntityFrameworkCore.Migrations;

namespace Tsw.EventBus.Outbox;

/// <summary>
/// Only for creation migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IntegrationEventLogContext>
{
  public IntegrationEventLogContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<IntegrationEventLogContext>();
    optionsBuilder.UseNpgsql(".", options =>
      {
        options.MigrationsAssembly(GetType().Assembly.GetName().Name);
        options.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "eventslog");
      });

    return new IntegrationEventLogContext(optionsBuilder.Options);
  }
}
