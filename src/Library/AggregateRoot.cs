using System.Text.Json.Serialization;

namespace Library;

/// <summary>
/// Represents an event in the event sourcing system.
/// </summary>
public record Event
{
    /// <summary>
    /// Gets the ID of the aggregate this event belongs to.
    /// </summary>
    public string AggregateId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="Event"/> record.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    [JsonConstructor]
    public Event(string aggregateId)
    {
        AggregateId = aggregateId;
    }
}

/// <summary>
/// Base class for aggregate roots in the event sourcing system.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<Event> _uncommittedChanges = [];

    /// <summary>
    /// Gets the ID of the aggregate.
    /// </summary>
    public string AggregateId { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets the current version of the aggregate.
    /// </summary>
    public int Version { get; protected set; } = -1;

    /// <summary>
    /// Raises an event and applies it to the aggregate.
    /// </summary>
    /// <param name="event">The event to raise.</param>
    protected void RaiseEvent(Event @event)
    {
        Apply(@event);
        _uncommittedChanges.Add(@event);
    }

    /// <summary>
    /// Applies the event to the aggregate state.
    /// </summary>
    /// <param name="event">The event to apply.</param>
    protected abstract void Apply(Event @event);

    /// <summary>
    /// Replays a sequence of events to restore the aggregate state.
    /// </summary>
    /// <param name="events">The events to replay.</param>
    public void ReplayEvents(IEnumerable<Event> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
            Version++;
        }
    }

    /// <summary>
    /// Gets the list of uncommitted changes.
    /// </summary>
    /// <returns>The uncommitted events.</returns>
    public IReadOnlyList<Event> GetUncommittedChanges() => _uncommittedChanges.AsReadOnly();

    /// <summary>
    /// Marks all uncommitted changes as committed.
    /// </summary>
    public void MarkChangesAsCommitted()
    {
        Version += _uncommittedChanges.Count;
        _uncommittedChanges.Clear();
    }
}