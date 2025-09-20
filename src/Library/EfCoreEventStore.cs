using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Library.Interfaces;

namespace Library;

/// <summary>
/// Entity model for storing events in the database.
/// </summary>
public class EventModel
{
    /// <summary>
    /// Gets or sets the unique ID of the event.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the ID of the aggregate.
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequence number of the event.
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized event data.
    /// </summary>
    public string EventData { get; set; } = string.Empty;
}

/// <summary>
/// DbContext for the event store.
/// </summary>
public class EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the events DbSet.
    /// </summary>
    public DbSet<EventModel> Events { get; set; } = null!;

    /// <summary>
    /// Configures the model for the event store.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventModel>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<EventModel>()
            .HasIndex(e => e.AggregateId);
        modelBuilder.Entity<EventModel>()
            .HasIndex(e => new { e.AggregateId, e.Sequence });
        modelBuilder.Entity<EventModel>()
            .Property(e => e.EventType)
            .IsRequired();
        modelBuilder.Entity<EventModel>()
            .Property(e => e.EventData)
            .IsRequired();
        modelBuilder.Entity<EventModel>()
            .Property(e => e.Timestamp)
            .IsRequired();
        modelBuilder.Entity<EventModel>()
            .Property(e => e.AggregateId)
            .IsRequired();
        modelBuilder.Entity<EventModel>()
            .Property(e => e.Sequence)
            .IsRequired();

        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>
/// Event store implementation using Entity Framework Core.
/// </summary>
public class EFCoreEventStore(EventStoreDbContext dbContext, IEventBus eventBus, ILogger<EFCoreEventStore> logger, JsonSerializerOptions options) : IEventStore
{

    /// <summary>
    /// Retrieves all events from the event store.
    /// </summary>
    /// <returns>A Maybe containing all events if any exist, otherwise None.</returns>
    public async Task<Maybe<IEnumerable<Event>>> GetAllEvents()
    {
        var eventModels = await dbContext.Events
            .AsNoTracking()
            .OrderBy(e => e.AggregateId)
            .ThenBy(e => e.Sequence)
            .ToListAsync();

        var events = eventModels
            .Select(e =>
            {
                try
                {
                    var type = Type.GetType(e.EventType) ?? throw new InvalidOperationException($"Type '{e.EventType}' not found.");
                    return JsonSerializer.Deserialize(e.EventData, type, options) as Event
                        ?? throw new InvalidOperationException($"Failed to deserialize event data for type '{e.EventType}'.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deserializing event {EventId} of type {EventType}", e.Id, e.EventType);
                    throw new InvalidOperationException($"Error deserializing event {e.Id} of type {e.EventType}: {ex.Message}", ex);
                }
            })
            .ToList();

        return events.Count != 0 ? Maybe<IEnumerable<Event>>.Some(events) : Maybe<IEnumerable<Event>>.None();
    }

    /// <summary>
    /// Retrieves all events for a specific aggregate.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    /// <returns>A Maybe containing the events if any exist, otherwise None.</returns>
    public async Task<Maybe<IEnumerable<Event>>> GetEventsForAggregateAsync(string aggregateId)
    {
        var eventModels = await dbContext.Events
            .AsNoTracking()
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Sequence)
            .ToListAsync();

        var events = eventModels
            .Select(e =>
            {
                try
                {
                    var type = Type.GetType(e.EventType) ?? throw new InvalidOperationException($"Type '{e.EventType}' not found.");
                    return JsonSerializer.Deserialize(e.EventData, type, options) as Event
                        ?? throw new InvalidOperationException($"Failed to deserialize event data for type '{e.EventType}'.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deserializing event {EventId} of type {EventType}", e.Id, e.EventType);
                    throw new InvalidOperationException($"Error deserializing event {e.Id} of type {e.EventType}: {ex.Message}", ex);
                }
            })
            .ToList();

        return events.Count != 0 ? Maybe<IEnumerable<Event>>.Some(events) : Maybe<IEnumerable<Event>>.None();
    }

    /// <summary>
    /// Saves a collection of events for an aggregate to the event store.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate.</param>
    /// <param name="events">The events to save.</param>
    /// <param name="expectedVersion">The expected version of the aggregate.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public async Task<Result> SaveEventsAsync(string aggregateId, IEnumerable<Event> events, int expectedVersion)
    {
        var currentVersion = await dbContext.Events
            .Where(e => e.AggregateId == aggregateId)
            .MaxAsync(e => (int?)e.Sequence) ?? -1;

        if (currentVersion != expectedVersion)
        {
            logger.LogWarning("Concurrency conflict for aggregate {AggregateId}: expected {Expected} but found {Found}", aggregateId, expectedVersion, currentVersion);
            return Result.Fail("Concurrency conflict: expected version " + expectedVersion + " but found " + currentVersion);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var eventList = events.ToList();
            var eventModels = new List<EventModel>();
            int sequence = expectedVersion + 1;

            foreach (var e in eventList)
            {
                eventModels.Add(new EventModel
                {
                    Id = Guid.NewGuid().ToString(),
                    AggregateId = aggregateId,
                    Sequence = sequence++,
                    EventType = e.GetType().AssemblyQualifiedName ?? throw new InvalidOperationException($"Event type for '{e.GetType().Name}' is null."),
                    EventData = JsonSerializer.Serialize(e, e.GetType(), options),
                    Timestamp = e.Timestamp
                });
            }

            await dbContext.Events.AddRangeAsync(eventModels);
            await dbContext.SaveChangesAsync();
            await eventBus.DispatchManyAsync(eventList);
            await transaction.CommitAsync();
            logger.LogInformation("Saved {Count} events for aggregate {AggregateId}", eventList.Count, aggregateId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to save events for aggregate {AggregateId}", aggregateId);
            return Result.Fail("Failed to save events: " + ex.Message);
        }
    }
}