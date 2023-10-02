namespace Tsw.Repository.Abstractions;

public interface IIdUniqueChecker<TId>
{
  Task<bool> IsUniqueId(
        TId uniqueId,
        bool noTracking = true,
        CancellationToken cancellationToken = default);
}
