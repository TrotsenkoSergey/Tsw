namespace Tsw.Repository.Abstractions;

public interface IReadRepository<TEntity, TId> : IIdUniqueChecker<TId>
  where TEntity : class, IIdentifiable<TId>
{
  Task<List<TEntity>> FindAsync(
    Specification<TEntity> specification,
    bool noTracking = true,
    CancellationToken ct = default);

  /// <summary>
  /// Get paged <see cref="TEntity"/>.
  /// </summary>
  /// <param name="specification"></param>
  /// <param name="pagination"></param>
  /// <param name="noTracking"></param>
  /// <param name="ct"></param>
  /// <returns>The pagination result with 0 or more <see cref="TEntity"/>.</returns>
  Task<PaginationResult<TEntity>> FindPagedAsync(
        Specification<TEntity> specification,
        Pagination pagination,
        bool noTracking = true,
        CancellationToken ct = default);

  Task<bool> AnyAsync(
    Specification<TEntity> specification, bool noTracking = true, CancellationToken ct = default);
}
