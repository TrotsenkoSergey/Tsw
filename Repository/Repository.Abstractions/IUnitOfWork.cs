namespace Tsw.Repository.Abstractions;

/// <summary>
/// Represents the unit of work interface.
/// </summary>
/// <typeparam name="TTransaction">DbContextTransaction type.</typeparam>
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
  /// <typeparam name="TTransaction">DbContextTransaction type.</typeparam>
  /// <returns>The new database context transaction or <see cref="null"/> if transaction exist.</returns>
  Task<TTransaction?> BeginTransactionAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Get <see cref="TTransaction"/> or <see cref="null"/> if there was no transaction.
  /// </summary>
  TTransaction? CurrentTransaction { get; }

  /// <summary>
  /// Commit <see cref="TTransaction"/>.
  /// </summary>
  /// <param name="transaction">The DbContextTransaction.</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task CommitTransactionAsync(TTransaction transaction, CancellationToken cancellationToken = default);

  /// <summary>
  /// Roll back <see cref="TTransaction"/>.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task RollbackAsync(CancellationToken cancellationToken = default);
}
