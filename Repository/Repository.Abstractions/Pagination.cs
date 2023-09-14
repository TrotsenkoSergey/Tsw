namespace Tsw.Repository.Abstractions;

public record Pagination(int Skip, int Take);

public record PaginationResult<TEntity>(int TotalCount, List<TEntity> Items);
