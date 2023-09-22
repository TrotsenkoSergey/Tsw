namespace Tsw.EventBus.Outbox;

/// <summary>
/// Only for creation migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IntegrationEventLogContext<TestDbContext>>
{
  public IntegrationEventLogContext<TestDbContext> CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
    optionsBuilder.UseNpgsql(".", options => 
      options.MigrationsAssembly(GetType().Assembly.GetName().Name));
    var testDb = new TestDbContext(optionsBuilder.Options);

    return new IntegrationEventLogContext<TestDbContext>(testDb);
  }
}

public class TestDbContext : DbContext
{ 
  public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}
