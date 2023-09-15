namespace Tsw.Repository.Abstractions;

public interface IIdUniqueChecker<TId>
  where TId : notnull
{
  Task<bool> IsUniqueId(
        TId uniqueId,
        bool noTracking = true,
        CancellationToken cancellationToken = default);
}
