namespace Library.Interfaces;

public interface IEventBus
{
    Task DispatchAsync(Event @event, CancellationToken cancellationToken = default);

    Task DispatchManyAsync(IEnumerable<Event> events, CancellationToken cancellationToken = default);
}