namespace Tsw.Repository.Abstractions;

public interface IWriteRepository<TEntity, TId>
  where TEntity : IIdentifiable<TId>
{
  TEntity Add(TEntity entity);

  void AddRange(IEnumerable<TEntity> entities);

  void Update(TEntity entity);

  void Remove(TEntity entity);

  /// <summary>
  /// Truncate all information into concrete table.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task<int> TruncateAsync(string tableName, CancellationToken cancellationToken = default);
}
