using System.Runtime.CompilerServices;

using Microsoft.EntityFrameworkCore;

using Tsw.Repository.Abstractions;

namespace Tsw.Repository.EFCore;

public class Repository<TEntity, TId> : IRepository<TEntity, TId>
  where TEntity : class, IIdentifiable<TId>
  where TId: notnull
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

  public virtual Task<List<TEntity>> FindAsync(
    Specification<TEntity> specification,
    bool noTracking,
    CancellationToken ct)
  {
    IQueryable<TEntity> query = GetQueryWithCondition(specification, noTracking);
    return query.ToListAsync(ct);
  }

  protected virtual IQueryable<TEntity> GetQueryWithCondition(
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

    if (specification.OrderByAsc is not null)
    {
      query = query.OrderBy(specification.OrderByAsc);
    }
    else if (specification.OrderByDesc is not null)
    {
      query = query.OrderByDescending(specification.OrderByDesc);
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
    int totalCount = await GetQuery(noTracking).CountAsync(ct);
    IQueryable<TEntity> query = GetQueryWithCondition(specification, noTracking, pagination);
    List<TEntity> entities = await query.ToListAsync(ct);
    return new PaginationResult<TEntity>(totalCount, entities);
  }

  public virtual Task<bool> AnyAsync(
    Specification<TEntity> specification, bool noTracking, CancellationToken ct)
  {
    IQueryable<TEntity> query = GetQueryWithCondition(specification, noTracking);
    return query.AnyAsync(ct);
  }

  public Task<TEntity?> LastOrDefault(bool noTracking = true, CancellationToken ct = default) =>
    GetQuery(noTracking).OrderBy(x => x.Id).LastOrDefaultAsync(ct);

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
}
