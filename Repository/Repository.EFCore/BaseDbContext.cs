using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Tsw.Repository.Abstractions;

namespace Tsw.Repository.EFCore;

public class BaseDbContext : DbContext, IUnitOfWork<IDbContextTransaction>, IDisposable
{
  private IDbContextTransaction? _currentTransaction;
  public virtual IDbContextTransaction? CurrentTransaction => _currentTransaction;

  public virtual async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
  {
    if (_currentTransaction is null)
    {
      _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
      return _currentTransaction;
    }

    return null;
  }

  public virtual async Task CommitTransactionAsync(
    IDbContextTransaction transaction,
    CancellationToken ct)
  {
    if (transaction == null)
    {
      throw new ArgumentNullException(nameof(transaction));
    }

    if (transaction != _currentTransaction)
    {
      throw new InvalidOperationException($"Transaction {transaction.TransactionId} is not current");
    }

    try
    {
      await SaveChangesAsync(ct);
      await transaction.CommitAsync(ct);
    }
    catch
    {
      await RollbackAsync(ct);
      throw;
    }
    finally
    {
      ((IDisposable)this).Dispose();
      _currentTransaction = null;
    }
  }

  public virtual async Task RollbackAsync(CancellationToken cancellationToken)
  {
    try
    {
      if (_currentTransaction is not null)
      {
        await _currentTransaction.RollbackAsync(cancellationToken);
      }
    }
    finally
    {
      ((IDisposable)this).Dispose();
      _currentTransaction = null;
    }
  }

  public override void Dispose()
  {
    _currentTransaction?.Dispose();
    base.Dispose();
  }
}
