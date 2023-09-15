namespace Tsw.Repository.Abstractions;

public interface IWriteRepository<TEntity, TId>
  where TEntity : class, IIdentifiable<TId>
  where TId : notnull
{
  TEntity Add(TEntity entity);

  void AddRange(IEnumerable<TEntity> entities);

  void Update(TEntity entity);

  void Remove(TEntity entity);

  Task<int> TruncateAsync(string tableName, CancellationToken cancellationToken = default);
}
