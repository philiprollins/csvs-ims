using Library;
using Xunit;

namespace Library.Tests;

public class TestAggregate : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;

    public TestAggregate() { } // Parameterless constructor for testing

    public TestAggregate(string id)
    {
        AggregateId = id;
    }

    public void ChangeName(string newName)
    {
        RaiseEvent(new NameChangedEvent(AggregateId, newName));
    }

    protected override void Apply(Event @event)
    {
        switch (@event)
        {
            case NameChangedEvent nameChanged:
                AggregateId = nameChanged.AggregateId; // Set AggregateId
                Name = nameChanged.NewName;
                break;
        }
    }
}

public record NameChangedEvent(string AggregateId, string NewName) : Event(AggregateId);

public class AggregateRootTests
{
    [Fact]
    public void RaiseEvent_AddsToUncommittedChanges()
    {
        var aggregate = new TestAggregate("test-id");
        aggregate.ChangeName("New Name");

        var changes = aggregate.GetUncommittedChanges();
        Assert.Single(changes);
        Assert.IsType<NameChangedEvent>(changes[0]);
        Assert.Equal("New Name", ((NameChangedEvent)changes[0]).NewName);
    }

    [Fact]
    public void RaiseEvent_AppliesEvent()
    {
        var aggregate = new TestAggregate("test-id");
        aggregate.ChangeName("New Name");

        Assert.Equal("New Name", aggregate.Name);
    }

    [Fact]
    public void ReplayEvents_AppliesAllEvents()
    {
        var aggregate = new TestAggregate("test-id");
        var events = new List<Event>
        {
            new NameChangedEvent("test-id", "First Name"),
            new NameChangedEvent("test-id", "Second Name")
        };

        aggregate.ReplayEvents(events);

        Assert.Equal("Second Name", aggregate.Name);
        Assert.Equal(1, aggregate.Version); // Starts at -1, +2 events
    }

    [Fact]
    public void MarkChangesAsCommitted_ClearsChangesAndUpdatesVersion()
    {
        var aggregate = new TestAggregate("test-id");
        aggregate.ChangeName("New Name");

        Assert.Single(aggregate.GetUncommittedChanges());
        Assert.Equal(-1, aggregate.Version);

        aggregate.MarkChangesAsCommitted();

        Assert.Empty(aggregate.GetUncommittedChanges());
        Assert.Equal(0, aggregate.Version); // -1 + 1 change
    }

    [Fact]
    public void GetUncommittedChanges_ReturnsReadOnlyList()
    {
        var aggregate = new TestAggregate("test-id");
        aggregate.ChangeName("New Name");

        var changes = aggregate.GetUncommittedChanges();
        Assert.IsAssignableFrom<IReadOnlyList<Event>>(changes);
    }
}