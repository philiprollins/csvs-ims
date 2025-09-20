namespace Library.Interfaces;

/// <summary>
/// Defines a handler for events.
/// </summary>
/// <typeparam name="TEvent">The type of the event.</typeparam>
public interface IEventHandler<in TEvent> where TEvent : Event
{
    /// <summary>
    /// Handles the event asynchronously.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}