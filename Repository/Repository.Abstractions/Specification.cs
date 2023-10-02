using System.Linq.Expressions;

namespace Tsw.Repository.Abstractions;

public abstract class Specification<TEntity>
{
  public IReadOnlyCollection<Expression<Func<TEntity, object>>> Includes => _includes;
  private readonly List<Expression<Func<TEntity, object>>> _includes = new();
  protected void AddInclude(Expression<Func<TEntity, object>> navigationPropertyPath)
  {
    _includes.Add(navigationPropertyPath);
  }

  public IReadOnlyCollection<string> ThenIncludes => _thenIncludes;
  private readonly List<string> _thenIncludes = new();
  protected void AddThenInclude(string navigationPropertyPath) // for deep include
  {
    _thenIncludes.Add(navigationPropertyPath);
  }

  public Expression<Func<TEntity, object>>? OrderByAsc { get; private set; }
  protected void AddOrderByAscending(Expression<Func<TEntity, object>> orderByExpression) =>
    OrderByAsc = orderByExpression;

  public Expression<Func<TEntity, object>>? OrderByDesc { get; private set; }
  protected void AddOrderByDescending(Expression<Func<TEntity, object>> orderByExpression) =>
    OrderByDesc = orderByExpression;

  public abstract IQueryable<TEntity> ApplyToTable(IQueryable<TEntity> table);
}
