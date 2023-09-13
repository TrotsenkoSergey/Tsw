namespace Tsw.Repository.Abstractions;

public record Pagination(int Skip, int Take);

public record PaginationResult<T>(int TotalCount, List<T> Items);
