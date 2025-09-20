using Library.Interfaces;

namespace Library;

/// <summary>
/// Repository for managing aggregates using event sourcing.
/// </summary>
/// <typeparam name="T">The type of the aggregate root.</typeparam>
public class AggregateRepository<T>(IEventStore eventStore) : IAggregateRepository<T> where T : AggregateRoot, new()
{
    /// <summary>
    /// Checks if an aggregate with the specified ID exists.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the aggregate exists, otherwise false.</returns>
    public async Task<bool> ExistsAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        var maybeEvents = await eventStore.GetEventsForAggregateAsync(aggregateId);

        return maybeEvents.HasValue && maybeEvents.Value.Any();
    }

    /// <summary>
    /// Retrieves an aggregate by its ID.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Maybe containing the aggregate if found, otherwise None.</returns>
    public async Task<Maybe<T>> GetByIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        var maybeEvents = await eventStore.GetEventsForAggregateAsync(aggregateId);

        if (maybeEvents.IsNone)
        {
            return Maybe<T>.None();
        }

        var aggregate = new T();
        aggregate.ReplayEvents(maybeEvents.Value);
        return Maybe<T>.Some(aggregate);
    }

    /// <summary>
    /// Saves the aggregate by persisting its uncommitted changes to the event store.
    /// </summary>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public async Task<Result> SaveAsync(T aggregate, CancellationToken cancellationToken = default)
    {
        var uncommittedChanges = aggregate.GetUncommittedChanges();

        if (uncommittedChanges.Count > 0)
        {
            var result = await eventStore.SaveEventsAsync(
                aggregate.AggregateId,
                uncommittedChanges,
                aggregate.Version);

            if (result.IsSuccess)
            {
                aggregate.MarkChangesAsCommitted();
            }

            return result;
        }

        return Result.Ok();
    }
}