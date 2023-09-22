using Npgsql;

namespace Tsw.EventBus.Outbox;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IntegrationEventLogContext>
{
  public IntegrationEventLogContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<IntegrationEventLogContext>();

    var dbConnection = new NpgsqlConnection(".");

    optionsBuilder.UseNpgsql(dbConnection, 
      options => options.MigrationsAssembly(GetType().Assembly.GetName().Name));

    return new IntegrationEventLogContext(dbConnection);
  }
}
