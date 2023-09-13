using System.Runtime.CompilerServices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Tsw.Repository.Abstractions;

namespace Tsw.Repository.EFCore;

public class Repository<TDbContext, TEntity> :
  IRepository<TEntity>, IUnitOfWork<IDbContextTransaction>, IDisposable
  where TDbContext : DbContext
  where TEntity : class
{
  protected readonly TDbContext _context;
  protected readonly string? _sharedEntityName;
  private IDbContextTransaction? _currentTransaction;

  public Repository(
    TDbContext context,
    string? sharedEntityName = default)
  {
    _context = context;
    _sharedEntityName = sharedEntityName;
  }

  public virtual Task<List<TEntity>> FindAsync(
    Specification<TEntity> specification,
    bool noTracking,
    CancellationToken ct)
  {
    var query = GetQuery(noTracking);
    query = specification.ApplyToTable(query); // order is important!
    query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
    query = specification.ThenIncludes.Aggregate(query, (current, include) => current.Include(include));
    return query.ToListAsync(ct);
  }

  public virtual TEntity Add(TEntity entity) =>
    DbSet.Add(entity).Entity;

  public virtual void AddRange(IEnumerable<TEntity> entities) =>
    DbSet.AddRange(entities);

  public virtual void Update(TEntity entity) =>
    DbSet.Update(entity);

  public virtual void Remove(TEntity entity) =>
    DbSet.Remove(entity);

  public Task<int> TruncateAsync(string tableName, CancellationToken cancellationToken)
  {
    var fs = FormattableStringFactory.Create(
        $"""
            TRUNCATE TABLE "{tableName}"
         """);
    return _context.Database.ExecuteSqlAsync(fs, cancellationToken);
  }

  public virtual Task<int> SaveChangesAsync(CancellationToken ct) =>
    _context.SaveChangesAsync(ct);

  protected virtual DbSet<TEntity> DbSet =>
    string.IsNullOrEmpty(_sharedEntityName)
      ? _context.Set<TEntity>()
      : _context.Set<TEntity>(_sharedEntityName);

  protected virtual IQueryable<TEntity> GetQuery(bool noTracking = true) =>
    noTracking ? DbSet.AsNoTracking() : DbSet;

  public virtual async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
  {
    if (_currentTransaction is null)
    {
      _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    return null;
  }

  public virtual IDbContextTransaction? CurrentTransaction => _currentTransaction;

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
      RollbackTransaction();
      throw;
    }
    finally
    {
      Dispose();
      _currentTransaction = null;
    }
  }

  public virtual void RollbackTransaction()
  {
    try
    {
      _currentTransaction?.Rollback();
    }
    finally
    {
      Dispose();
      _currentTransaction = null;
    }
  }

  public virtual void Dispose()
  {
    _currentTransaction?.Dispose();
  }
}
