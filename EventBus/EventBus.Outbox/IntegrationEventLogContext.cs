using System.Data.Common;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tsw.EventBus.Outbox;

public class IntegrationEventLogContext<ApplicationDbContext> : DbContext
  where ApplicationDbContext : DbContext
{
  private readonly DbConnection _connection;

  public IntegrationEventLogContext(ApplicationDbContext applicationDbContext)
  {
    _connection = applicationDbContext.Database.GetDbConnection();
  }

  public DbSet<IntegrationEventLog> IntegrationEventLogs { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.UseNpgsql(
      _connection,
      opt =>
      {
        opt.EnableRetryOnFailure(
          maxRetryCount: 15,
          maxRetryDelay: TimeSpan.FromSeconds(30),
          errorCodesToAdd: null);
        opt.MigrationsAssembly(typeof(IntegrationEventLogContext<>).Assembly.FullName);
        opt.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "eventslog");
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
