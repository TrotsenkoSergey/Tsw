using System.Runtime.CompilerServices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Tsw.Repository.Abstractions;

namespace Tsw.Repository.EFCore;

public class Repository<TDbContext, TEntity, TId> :
  IRepository<TEntity, TId>, IUnitOfWork<IDbContextTransaction>, IDisposable
  where TDbContext : DbContext
  where TEntity : class, IIdentifiable<TId>
  where TId : notnull
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

  public async Task<bool> IsUniqueId(
    TId uniqueId,
    bool noTracking,
    CancellationToken ct) =>
  !await GetQuery(noTracking).AnyAsync(x => x.Id.Equals(uniqueId));

  public virtual Task<List<TEntity>> FindAsync(
    Specification<TEntity> specification,
    bool noTracking,
    CancellationToken ct)
  {
    IQueryable<TEntity> query = GetQueryWithCondition(specification, noTracking);
    return query.ToListAsync(ct);
  }

  private IQueryable<TEntity> GetQueryWithCondition(
    Specification<TEntity> specification,
    bool noTracking,
    Pagination? pagination = default)
  {
    IQueryable<TEntity> query = GetQuery(noTracking);
    query = specification.ApplyToTable(query); // order is important!

    if (pagination is not null)
    {
      query = query.OrderBy(x => x.Id)
                   .Skip(pagination.Skip)
                   .Take(pagination.Take);
    }

    query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
    query = specification.ThenIncludes.Aggregate(query, (current, include) => current.Include(include));
    return query;
  }

  /// <inheritdoc />
  public virtual async Task<PaginationResult<TEntity>> FindPagedAsync(
        Specification<TEntity> specification,
        Pagination pagination,
        bool noTracking,
        CancellationToken ct)
  {
    int totalCount = await GetQuery(noTracking).CountAsync(ct);
    IQueryable<TEntity> query = GetQueryWithCondition(specification, noTracking, pagination);
    List<TEntity> entities = await query.ToListAsync(ct);
    return new PaginationResult<TEntity>(totalCount, entities);
  }

  public virtual Task<bool> AnyAsync(CancellationToken ct = default) =>
    GetQuery().AnyAsync(ct);

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
      await RollbackAsync(ct);
      throw;
    }
    finally
    {
      Dispose();
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
      Dispose();
      _currentTransaction = null;
    }
  }

  public virtual void Dispose()
  {
    _currentTransaction?.Dispose();
  }
}
