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

  private Guid? _currentTransactionId;
  public virtual Guid? CurrentTransactionId => _currentTransactionId;

  public virtual async Task<DbTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
  {
    if (_currentTransactionId is null)
    {
      var transaction = await Database.BeginTransactionAsync(cancellationToken);
      _currentTransactionId = transaction.TransactionId;
      return transaction.GetDbTransaction();
    }

    return null;
  }

  public DatabaseFacade DataBase => Database;

  public virtual async Task CommitTransactionAsync(
    DbTransaction transaction,
    Guid currentTransactionId,
    CancellationToken ct)
  {
    if (transaction == null)
    {
      throw new ArgumentNullException(nameof(transaction));
    }

    if (currentTransactionId != _currentTransactionId)
    {
      throw new InvalidOperationException($"Transaction {currentTransactionId} is not current");
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
      _currentTransactionId = null;
    }
  }

  public virtual async Task RollbackAsync(DbTransaction transaction, CancellationToken cancellationToken)
  {
    try
    {
        await transaction.RollbackAsync(cancellationToken);
    }
    finally
    {
      ((IDisposable)this).Dispose();
      _currentTransactionId = null;
    }
  }
}
