using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using Tsw.Repository.Abstractions;

namespace Tsw.Repository.EFCore;

public class BaseDbContext : DbContext, IUnitOfWork<DatabaseFacade>
{
  public BaseDbContext(DbContextOptions options) : base(options)
  {
  }

  public virtual async Task<DbTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
  {
    var transaction = await Database.BeginTransactionAsync(cancellationToken);
    return transaction?.GetDbTransaction();
  }

  public DatabaseFacade DataBase => Database;

  public virtual async Task CommitTransactionAsync(DbTransaction transaction, CancellationToken ct)
  {
    if (transaction is null)
    {
      throw new ArgumentNullException(nameof(transaction));
    }

    try
    {
      await SaveChangesAsync(ct);
      await transaction.CommitAsync(ct);
    }
    catch
    {
      await RollbackAsync(transaction, ct);
      throw;
    }
    finally
    {
      ((IDisposable)this).Dispose();
    }
  }

  public virtual async Task RollbackAsync(DbTransaction transaction, CancellationToken cancellationToken)
  {
    try
    {
      await transaction.RollbackAsync(cancellationToken);
    }
    catch 
    {
      throw;
    }
    finally
    {
      ((IDisposable)this).Dispose();
    }
  }
}
