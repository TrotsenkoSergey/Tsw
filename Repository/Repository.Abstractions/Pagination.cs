namespace Tsw.Repository.Abstractions;

public record Pagination
{
  private readonly int _skip;
  public int Skip => _skip;
  private readonly int _take;
  public int Take => _take;

  public Pagination(int page, int pageSize)
  {
    _skip = (page - 1) * pageSize;
    _take = pageSize;
  }
}

public record PaginationResult<TEntity>(int TotalCount, List<TEntity> Items);
