namespace Tsw.EventBus.Outbox;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IntegrationEventLogContext>
{
  public IntegrationEventLogContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<IntegrationEventLogContext>();

    optionsBuilder.UseNpgsql(".", options => options.MigrationsAssembly(GetType().Assembly.GetName().Name));

    return new IntegrationEventLogContext(optionsBuilder.Options);
  }
}
