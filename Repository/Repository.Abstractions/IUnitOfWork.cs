namespace Tsw.Repository.Abstractions;

/// <summary>
/// Represents the unit of work interface.
/// </summary>
public interface IUnitOfWork<TTransaction>
  where TTransaction : class
{
  /// <summary>
  /// Saves all of the pending changes in the unit of work.
  /// </summary>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The number of entities that have been saved.</returns>
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Begins a transaction on the current unit of work.
  /// </summary>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The new database context transaction.</returns>
  Task<TTransaction?> BeginTransactionAsync(CancellationToken cancellationToken = default);

  TTransaction? CurrentTransaction { get; }

  Task CommitTransactionAsync(TTransaction transaction, CancellationToken cancellationToken = default);

  void RollbackTransaction();
}
