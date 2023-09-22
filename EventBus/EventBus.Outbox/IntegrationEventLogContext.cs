using System.Data.Common;

namespace Tsw.EventBus.Outbox;

public class IntegrationEventLogContext : DbContext
{
  private readonly IServiceProvider _sp;

  //public IntegrationEventLogContext(
  //  DbContextOptions<IntegrationEventLogContext> options) : base(options)
  //{
  //}

  public IntegrationEventLogContext(IServiceProvider sp)
  {
    _sp = sp;
  }

  public DbSet<IntegrationEventLog> IntegrationEventLogs { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    var dbConnection = _sp.GetRequiredService<DbConnection>();
    optionsBuilder.UseNpgsql(
      dbConnection,
      opt =>
      {
        opt.EnableRetryOnFailure(
          maxRetryCount: 15,
          maxRetryDelay: TimeSpan.FromSeconds(30),
          errorCodesToAdd: null);
        opt.MigrationsAssembly(typeof(IntegrationEventLogContext).Assembly.FullName);
      });
    base.OnConfiguring(optionsBuilder);
  }

  protected override void OnModelCreating(ModelBuilder builder)
  {
    builder.Entity<IntegrationEventLog>(ConfigureIntegrationEventLogEntry);
  }

  void ConfigureIntegrationEventLogEntry(
    EntityTypeBuilder<IntegrationEventLog> builder)
  {
    builder.ToTable(nameof(IntegrationEventLogs));

    builder.HasKey(e => e.EventId);

    builder.Property(e => e.Content)
      .HasMaxLength(500)
      .IsRequired();

    builder.Property(e => e.CreatedOnUtc).IsRequired();

    builder.Property(e => e.State).IsRequired();

    builder.Property(e => e.TimesSent).IsRequired();

    builder.Property(e => e.EventTypeName)
      .HasMaxLength(50)
      .IsRequired();

    builder.Property(e => e.TransactionId)
      .HasMaxLength(50)
      .IsRequired();

    builder.Ignore(e => e.EventTypeShortName);

    builder.Ignore(e => e.IntegrationEvent);
  }
}
