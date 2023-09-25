namespace Tsw.Repository.Abstractions;

public class EmptySpecification<TEntity> : Specification<TEntity>
{
  public override IQueryable<TEntity> ApplyToTable(IQueryable<TEntity> table) => table;
}
