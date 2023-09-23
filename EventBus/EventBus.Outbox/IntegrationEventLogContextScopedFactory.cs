using System.Data.Common;

using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;

namespace Tsw.EventBus.Outbox;

internal class IntegrationEventLogContextScopedFactory<MainDbContext> : IDbContextFactory<IntegrationEventLogContext>
  where MainDbContext : DbContext
{
  private readonly ILogger<IntegrationEventLogContextScopedFactory<MainDbContext>> _logger;
  private readonly IServiceProvider _serviceProvider;
  private readonly IDbContextFactory<IntegrationEventLogContext> _pooledFactory;

  public IntegrationEventLogContextScopedFactory(
        ILogger<IntegrationEventLogContextScopedFactory<MainDbContext>> logger,
        IServiceProvider serviceProvider,
        IDbContextFactory<IntegrationEventLogContext> pooledFactory)
  {
    _logger = logger;
    _serviceProvider = serviceProvider;
    _pooledFactory = pooledFactory;
  }

  public IntegrationEventLogContext CreateDbContext()
  {
    var context = _pooledFactory.CreateDbContext();

    var scope = _serviceProvider.CreateScope();
    try
    {
      var mainContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
      DbConnection connection = mainContext.Database.GetDbConnection();
      context.Database.SetDbConnection(connection);
      _logger.LogInformation("Using connection from main DbContext: {connection}.", connection.ConnectionString);
    }
    catch (ObjectDisposedException ex)
    {
      _logger.LogInformation($"{ex.Message}");
      _logger.LogInformation($"Using own connection from {nameof(IntegrationEventLogContext)}.");
    }
    finally
    {
      scope.Dispose();
    }

    return context;
  }
}
