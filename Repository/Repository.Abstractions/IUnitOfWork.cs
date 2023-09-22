using System.Data.Common;

namespace Tsw.Repository.Abstractions;

/// <summary>
/// Represents the unit of work interface.
/// </summary>
/// <typeparam name="DatabaseFacade">DatabaseFacade type.</typeparam>
public interface IUnitOfWork<DatabaseFacade>
  where DatabaseFacade : class
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
  /// Get <see cref="Guid"/> or <see cref="null"/> if there was no transaction.
  /// </summary>
  Guid? CurrentTransactionId { get; }

  /// <summary>
  /// Get DatabaseFacade.
  /// </summary>
  DatabaseFacade DataBase { get; }

  /// <summary>
  /// Commit <see cref="DbTransaction"/>.
  /// </summary>
  /// <param name="transaction">The DbContextTransaction.</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task CommitTransactionAsync(
    DbTransaction transaction, Guid currentTransactionId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Roll back <see cref="TTransaction"/>.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task RollbackAsync(DbTransaction transaction, CancellationToken cancellationToken = default);
}
