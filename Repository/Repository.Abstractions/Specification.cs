using System.Linq.Expressions;

namespace Tsw.Repository.Abstractions;

public abstract class Specification<TEntity>
{
  private readonly List<Expression<Func<TEntity, object>>> _includes = new();
  private readonly List<string> _thenIncludes = new();

  public IReadOnlyCollection<Expression<Func<TEntity, object>>> Includes => _includes;

  public IReadOnlyCollection<string> ThenIncludes => _thenIncludes;

  protected void AddInclude(Expression<Func<TEntity, object>> navigationPropertyPath)
  {
    _includes.Add(navigationPropertyPath);
  }

  protected void AddThenInclude(string navigationPropertyPath) // for deep include
  {
    _thenIncludes.Add(navigationPropertyPath);
  }

  public abstract IQueryable<TEntity> ApplyToTable(IQueryable<TEntity> table);
}
