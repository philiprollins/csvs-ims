namespace Library.Interfaces;

/// <summary>
/// Defines an event store for persisting and retrieving events.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Retrieves all events from the event store.
    /// </summary>
    /// <returns>A Maybe containing all events if any exist, otherwise None.</returns>
    Task<Maybe<IEnumerable<Event>>> GetAllEvents();

    /// <summary>
    /// Retrieves all events for a specific aggregate.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    /// <returns>A Maybe containing the events if any exist, otherwise None.</returns>
    Task<Maybe<IEnumerable<Event>>> GetEventsForAggregateAsync(string aggregateId);

    /// <summary>
    /// Saves events for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    /// <param name="events">The events to save.</param>
    /// <param name="expectedVersion">The expected version of the aggregate.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> SaveEventsAsync(string aggregateId, IEnumerable<Event> events, int expectedVersion);
}