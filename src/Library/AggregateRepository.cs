using Library.Interfaces;

namespace Library;

public class AggregateRepository<T>(IEventStore eventStore) : IAggregateRepository<T> where T : AggregateRoot, new()
{
    public async Task<bool> ExistsAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        var maybeEvents = await eventStore.GetEventsForAggregateAsync(aggregateId);

        return maybeEvents.HasValue && maybeEvents.Value.Any();
    }

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