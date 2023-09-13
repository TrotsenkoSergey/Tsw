namespace Tsw.Repository.Abstractions;

public interface IReadRepository<TEntity, TId>
  where TEntity : IIdentifiable<TId>
{
  Task<List<TEntity>> FindAsync(
    Specification<TEntity> specification,
    bool noTracking = true,
    CancellationToken ct = default);

  Task<PaginationResult<TEntity>> FindPagedAsync(
        Specification<TEntity> specification,
        Pagination pagination,
        bool noTracking = true,
        CancellationToken ct = default);

  Task<bool> AnyAsync(CancellationToken ct = default);
}
