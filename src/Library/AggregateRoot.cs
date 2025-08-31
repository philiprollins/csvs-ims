using System.Text.Json.Serialization;

namespace Library;

public record Event
{
    public string AggregateId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    [JsonConstructor]
    public Event(string aggregateId)
    {
        AggregateId = aggregateId;
    }
}

public abstract class AggregateRoot
{
    private readonly List<Event> _uncommittedChanges = [];

    public string AggregateId { get; protected set; } = string.Empty;

    public int Version { get; protected set; } = -1;

    protected void RaiseEvent(Event @event)
    {
        Apply(@event);
        _uncommittedChanges.Add(@event);
    }

    protected abstract void Apply(Event @event);

    public void ReplayEvents(IEnumerable<Event> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
            Version++;
        }
    }

    public IReadOnlyList<Event> GetUncommittedChanges() => _uncommittedChanges.AsReadOnly();

    public void MarkChangesAsCommitted()
    {
        Version += _uncommittedChanges.Count;
        _uncommittedChanges.Clear();
    }
}