using Library.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Library;

public class InMemoryEventBus(IServiceProvider provider, ILogger<InMemoryEventBus> logger) : IEventBus
{
    private readonly IServiceProvider _provider = provider;

    public async Task DispatchAsync(Event @event, CancellationToken cancellationToken = default)
    {
        var eventType = @event.GetType();

        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);

        var handlers = _provider.GetServices(handlerType);

        if (!handlers.Any())
        {
            logger.LogDebug("No handlers found for event type {EventType}", eventType.Name);
            return;
        }

        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("HandleAsync") ?? throw new InvalidOperationException($"Handler for event type {eventType.Name} does not implement HandleAsync method.");
            try
            {
                var task = method.Invoke(handler, [@event, cancellationToken]) as Task ?? throw new InvalidOperationException($"Handler for event type {eventType.Name} did not return a Task.");
                tasks.Add(task);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error invoking handler for event type {EventType}", eventType.Name);
                throw;
            }
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task DispatchManyAsync(IEnumerable<Event> events, CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            await DispatchAsync(@event, cancellationToken).ConfigureAwait(false);
        }
    }
}