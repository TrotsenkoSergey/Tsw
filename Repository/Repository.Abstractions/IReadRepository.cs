namespace Tsw.Repository.Abstractions;

public interface IReadRepository<TEntity>
    where TEntity : class
{
  Task<List<TEntity>> FindAsync(
    Specification<TEntity> specification,
    bool noTracking = true,
    CancellationToken ct = default);
}
