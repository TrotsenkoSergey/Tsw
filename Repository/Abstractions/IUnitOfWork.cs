using System.Data.Common;

namespace Tsw.Repository.Abstractions;

/// <summary>
/// Represents the unit of work interface.
/// </summary>
/// <typeparam name="TDatabase">Database type (e.g. DatabaseFacade).</typeparam>
public interface IUnitOfWork<TDatabase>
  where TDatabase : class
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
  /// <returns>The new database context transaction or <see cref="null"/> if transaction exist.</returns>
  Task<DbTransaction?> BeginTransactionAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Get Database.
  /// </summary>
  TDatabase DataBase { get; }

  /// <summary>
  /// Commit <see cref="DbTransaction"/>.
  /// </summary>
  /// <param name="transaction">The DbContextTransaction.</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task CommitTransactionAsync(
    DbTransaction transaction, CancellationToken cancellationToken = default);

  /// <summary>
  /// Roll back <see cref="DbTransaction"/>.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task RollbackAsync(DbTransaction transaction, CancellationToken cancellationToken = default);
}
