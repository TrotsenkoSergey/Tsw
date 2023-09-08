namespace Tsw.EventBus.Outbox;

public class IntegrationEventLogContext : DbContext
{
  public IntegrationEventLogContext(
    DbContextOptions<IntegrationEventLogContext> options) : base(options)
  {
  }

  public DbSet<IntegrationEventLog> IntegrationEventLogs { get; set; }

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
