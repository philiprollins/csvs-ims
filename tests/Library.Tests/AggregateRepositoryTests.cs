using Library;
using Library.Interfaces;
using Moq;
using Xunit;

namespace Library.Tests;

public class AggregateRepositoryTests
{
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly AggregateRepository<TestAggregate> _repository;

    public AggregateRepositoryTests()
    {
        _eventStoreMock = new Mock<IEventStore>();
        _repository = new AggregateRepository<TestAggregate>(_eventStoreMock.Object);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenEventsExist()
    {
        _eventStoreMock.Setup(es => es.GetEventsForAggregateAsync("test-id"))
            .ReturnsAsync(Maybe<IEnumerable<Event>>.Some(new List<Event> { new NameChangedEvent("test-id", "Name") }));

        var exists = await _repository.ExistsAsync("test-id");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenNoEvents()
    {
        _eventStoreMock.Setup(es => es.GetEventsForAggregateAsync("test-id"))
            .ReturnsAsync(Maybe<IEnumerable<Event>>.None());

        var exists = await _repository.ExistsAsync("test-id");

        Assert.False(exists);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAggregate_WhenEventsExist()
    {
        var events = new List<Event> { new NameChangedEvent("test-id", "Name") };
        _eventStoreMock.Setup(es => es.GetEventsForAggregateAsync("test-id"))
            .ReturnsAsync(Maybe<IEnumerable<Event>>.Some(events));

        var result = await _repository.GetByIdAsync("test-id");

        Assert.True(result.HasValue);
        Assert.Equal("test-id", result.Value.AggregateId);
        Assert.Equal("Name", result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNone_WhenNoEvents()
    {
        _eventStoreMock.Setup(es => es.GetEventsForAggregateAsync("test-id"))
            .ReturnsAsync(Maybe<IEnumerable<Event>>.None());

        var result = await _repository.GetByIdAsync("test-id");

        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task SaveAsync_SavesEvents_WhenUncommittedChangesExist()
    {
        var aggregate = new TestAggregate("test-id");
        aggregate.ChangeName("New Name");

        _eventStoreMock.Setup(es => es.SaveEventsAsync("test-id", It.IsAny<IEnumerable<Event>>(), -1))
            .ReturnsAsync(Result.Ok());

        var result = await _repository.SaveAsync(aggregate);

        Assert.True(result.IsSuccess);
        _eventStoreMock.Verify(es => es.SaveEventsAsync("test-id", It.IsAny<IEnumerable<Event>>(), -1), Times.Once);
        Assert.Empty(aggregate.GetUncommittedChanges());
        Assert.Equal(0, aggregate.Version);
    }

    [Fact]
    public async Task SaveAsync_ReturnsOk_WhenNoChanges()
    {
        var aggregate = new TestAggregate("test-id");

        var result = await _repository.SaveAsync(aggregate);

        Assert.True(result.IsSuccess);
        _eventStoreMock.Verify(es => es.SaveEventsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<Event>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_ReturnsFailure_WhenSaveFails()
    {
        var aggregate = new TestAggregate("test-id");
        aggregate.ChangeName("New Name");

        _eventStoreMock.Setup(es => es.SaveEventsAsync("test-id", It.IsAny<IEnumerable<Event>>(), -1))
            .ReturnsAsync(Result.Fail("Save failed"));

        var result = await _repository.SaveAsync(aggregate);

        Assert.True(result.IsFailure);
        Assert.Equal("Save failed", result.Errors["Error"]);
        Assert.Single(aggregate.GetUncommittedChanges()); // Changes not committed
    }
}