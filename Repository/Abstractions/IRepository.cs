namespace Tsw.Repository.Abstractions;

public interface IRepository<TEntity, TId> : IReadRepository<TEntity, TId>, IWriteRepository<TEntity, TId>
  where TEntity : class, IIdentifiable<TId>
{
}
