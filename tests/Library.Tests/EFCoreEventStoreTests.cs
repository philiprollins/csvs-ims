using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Library.Tests;

public class EFCoreEventStoreTests : IDisposable
{
    private readonly EventStoreDbContext _dbContext;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<EFCoreEventStore>> _loggerMock;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly EFCoreEventStore _eventStore;

    public EFCoreEventStoreTests()
    {
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new EventStoreDbContext(options);
        _eventBusMock = new Mock<IEventBus>();
        _loggerMock = new Mock<ILogger<EFCoreEventStore>>();
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        _eventStore = new EFCoreEventStore(_dbContext, _eventBusMock.Object, _loggerMock.Object, _jsonOptions);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task SaveEventsAsync_SavesEventsSuccessfully()
    {
        var events = new List<Event> { new NameChangedEvent("test-id", "New Name") };

        var result = await _eventStore.SaveEventsAsync("test-id", events, -1);

        Assert.True(result.IsSuccess);
        var savedEvents = await _dbContext.Events.Where(e => e.AggregateId == "test-id").ToListAsync();
        Assert.Single(savedEvents);
        Assert.Equal("Library.Tests.NameChangedEvent, Library.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", savedEvents[0].EventType);
    }

    [Fact]
    public async Task GetEventsForAggregateAsync_ReturnsEvents_WhenExist()
    {
        var eventModel = new EventModel
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = "test-id",
            Sequence = 0,
            EventType = typeof(NameChangedEvent).AssemblyQualifiedName!,
            EventData = JsonSerializer.Serialize(new NameChangedEvent("test-id", "Name"), _jsonOptions),
            Timestamp = DateTime.UtcNow
        };
        _dbContext.Events.Add(eventModel);
        await _dbContext.SaveChangesAsync();

        var result = await _eventStore.GetEventsForAggregateAsync("test-id");

        Assert.True(result.HasValue);
        var events = result.Value.ToList();
        Assert.Single(events);
        Assert.IsType<NameChangedEvent>(events[0]);
        Assert.Equal("Name", ((NameChangedEvent)events[0]).NewName);
    }

    [Fact]
    public async Task GetEventsForAggregateAsync_ReturnsNone_WhenNoEvents()
    {
        var result = await _eventStore.GetEventsForAggregateAsync("non-existent");

        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task SaveEventsAsync_FailsOnConcurrencyConflict()
    {
        // Add existing event
        var existingEvent = new EventModel
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = "test-id",
            Sequence = 0,
            EventType = typeof(NameChangedEvent).AssemblyQualifiedName!,
            EventData = JsonSerializer.Serialize(new NameChangedEvent("test-id", "Old Name"), _jsonOptions),
            Timestamp = DateTime.UtcNow
        };
        _dbContext.Events.Add(existingEvent);
        await _dbContext.SaveChangesAsync();

        var newEvents = new List<Event> { new NameChangedEvent("test-id", "New Name") };

        var result = await _eventStore.SaveEventsAsync("test-id", newEvents, -1); // Expected version -1, but current is 0

        Assert.True(result.IsFailure);
        Assert.Contains("Concurrency conflict", result.Errors["Error"]);
    }

    [Fact]
    public async Task GetAllEvents_ReturnsAllEvents()
    {
        var event1 = new EventModel
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = "agg1",
            Sequence = 0,
            EventType = typeof(NameChangedEvent).AssemblyQualifiedName!,
            EventData = JsonSerializer.Serialize(new NameChangedEvent("agg1", "Name1"), _jsonOptions),
            Timestamp = DateTime.UtcNow
        };
        var event2 = new EventModel
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = "agg2",
            Sequence = 0,
            EventType = typeof(NameChangedEvent).AssemblyQualifiedName!,
            EventData = JsonSerializer.Serialize(new NameChangedEvent("agg2", "Name2"), _jsonOptions),
            Timestamp = DateTime.UtcNow
        };
        _dbContext.Events.AddRange(event1, event2);
        await _dbContext.SaveChangesAsync();

        var result = await _eventStore.GetAllEvents();

        Assert.True(result.HasValue);
        var events = result.Value.ToList();
        Assert.Equal(2, events.Count);
    }
}