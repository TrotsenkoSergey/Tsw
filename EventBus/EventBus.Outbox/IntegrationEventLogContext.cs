﻿namespace Tsw.EventBus.Outbox;

public class IntegrationEventLogContext : DbContext
{
  public const int EventTypeNameMaxLength = 100;

  public IntegrationEventLogContext(DbContextOptions<IntegrationEventLogContext> options) : base(options)
  {
  }

  public DbSet<IntegrationEventLog> IntegrationEventLogs { get; set; }

  protected override void OnModelCreating(ModelBuilder builder)
  {
    builder.Entity<IntegrationEventLog>(ConfigureIntegrationEventLogEntry);
    builder.ApplyUtcDateTimeConverter();

    builder.ApplyEnumTableBuilding<EventState>();
  }

  void ConfigureIntegrationEventLogEntry(EntityTypeBuilder<IntegrationEventLog> builder)
  {
    builder.ToTable(nameof(IntegrationEventLogs));

    builder.HasKey(e => e.EventId);

    builder.Property(e => e.Content).IsRequired();

    builder.Property(e => e.CreatedOnUtc).IsRequired();

    builder.Property(e => e.State)
      .HasConversion<short>()
      .IsRequired();

    builder.Property(e => e.TimesSent).IsRequired();

    builder.Property(e => e.EventTypeName).HasMaxLength(EventTypeNameMaxLength).IsRequired();

    builder.Ignore(e => e.EventTypeShortName);

    builder.Ignore(e => e.IntegrationEvent);
  }
}
