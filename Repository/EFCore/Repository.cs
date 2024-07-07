using System.Runtime.CompilerServices;

using Microsoft.EntityFrameworkCore;

using Tsw.Repository.Abstractions;

namespace Tsw.Repository.EFCore;

public class Repository<TEntity, TId> : IRepository<TEntity, TId>
  where TEntity : class, IIdentifiable<TId>
  where TId : notnull
{
  protected readonly DbContext _context;
  protected readonly string? _sharedEntityName;

  public Repository(
    DbContext context,
    string? sharedEntityName = default)
  {
    _context = context;
    _sharedEntityName = sharedEntityName;
  }

  /// <inheritdoc />
  public async Task<bool> IsUniqueId(
    TId uniqueId,
    bool noTracking,
    CancellationToken ct) =>
  !await GetQuery(noTracking).AnyAsync(x => x.Id.Equals(uniqueId));

  /// <inheritdoc />
  public virtual Task<List<TEntity>> FindAsync(
    Specification<TEntity> specification,
    bool noTracking,
    CancellationToken ct)
  {
    IQueryable<TEntity> query = GetQueryWithSpecification(specification, noTracking);

    return query.ToListAsync(ct);
  }

  protected virtual IQueryable<TEntity> GetQueryWithSpecification(
    Specification<TEntity> specification,
    bool noTracking)
  {
    IQueryable<TEntity> query = GetQuery(noTracking);// order is important!

    query = specification.ApplyToTable(query);

    query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
    query = specification.ThenIncludes.Aggregate(query, (current, include) => current.Include(include));

    if (specification.OrderByAsc is not null)
    {
      query = query.OrderBy(specification.OrderByAsc);
    }
    else if (specification.OrderByDesc is not null)
    {
      query = query.OrderByDescending(specification.OrderByDesc);
    }
    else
    {
      query = query.OrderBy(x => x.Id);
    }

    return query;
  }

  /// <inheritdoc />
  public virtual async Task<PaginationResult<TEntity>> FindPagedAsync(
        Specification<TEntity> specification,
        Pagination pagination,
        bool noTracking,
        CancellationToken ct)
  {
    var query = GetQueryWithSpecification(specification, noTracking);

    int totalCount = await query.CountAsync(ct);

    query = query.Skip(pagination.Skip).Take(pagination.Take);

    List<TEntity> entities = await query.ToListAsync(ct);

    return new PaginationResult<TEntity>(totalCount, entities);
  }

  /// <inheritdoc />
  public virtual Task<bool> AnyAsync(
    Specification<TEntity> specification, bool noTracking, CancellationToken ct)
  {
    IQueryable<TEntity> query = GetQueryWithSpecification(specification, noTracking);
    return query.AnyAsync(ct);
  }

  /// <inheritdoc />
  public Task<TEntity?> LastOrDefault(bool noTracking = true, CancellationToken ct = default) =>
    GetQuery(noTracking).OrderBy(x => x.Id).LastOrDefaultAsync(ct);

  /// <inheritdoc />
  public virtual TEntity Add(TEntity entity) =>
    DbSet.Add(entity).Entity;

  /// <inheritdoc />
  public virtual void AddRange(IEnumerable<TEntity> entities) =>
    DbSet.AddRange(entities);

  /// <inheritdoc />
  public virtual void Update(TEntity entity) =>
    DbSet.Update(entity);

  /// <inheritdoc />
  public virtual void Remove(TEntity entity) =>
    DbSet.Remove(entity);

  /// <inheritdoc />
  public Task<int> TruncateAsync(string tableName, CancellationToken cancellationToken)
  {
    var fs = FormattableStringFactory.Create(
        $"""
            TRUNCATE TABLE "{tableName}"
         """);
    return _context.Database.ExecuteSqlAsync(fs, cancellationToken);
  }

  /// <inheritdoc />
  public virtual Task<int> SaveChangesAsync(CancellationToken ct) =>
    _context.SaveChangesAsync(ct);

  protected virtual DbSet<TEntity> DbSet =>
    string.IsNullOrEmpty(_sharedEntityName)
      ? _context.Set<TEntity>()
      : _context.Set<TEntity>(_sharedEntityName);

  protected virtual IQueryable<TEntity> GetQuery(bool noTracking = true) =>
    noTracking ? DbSet.AsNoTracking() : DbSet;
}
