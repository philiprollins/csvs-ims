namespace Library.Interfaces;

/// <summary>
/// Defines an event bus for dispatching events.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Dispatches a single event.
    /// </summary>
    /// <param name="event">The event to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DispatchAsync(Event @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches multiple events.
    /// </summary>
    /// <param name="events">The events to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DispatchManyAsync(IEnumerable<Event> events, CancellationToken cancellationToken = default);
}