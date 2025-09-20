namespace Library.Interfaces;

/// <summary>
/// Defines a repository for aggregates.
/// </summary>
/// <typeparam name="T">The type of the aggregate root.</typeparam>
public interface IAggregateRepository<T> where T : AggregateRoot, new()
{
    /// <summary>
    /// Retrieves an aggregate by its ID.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Maybe containing the aggregate if found, otherwise None.</returns>
    Task<Maybe<T>> GetByIdAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an aggregate with the specified ID exists.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the aggregate exists, otherwise false.</returns>
    Task<bool> ExistsAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the aggregate.
    /// </summary>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> SaveAsync(T aggregate, CancellationToken cancellationToken = default);
}