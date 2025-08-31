using Application.Features.Part;
using Application.Features.Part.Commands;
using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Tests.Features.Part.Commands;

public class DefinePartCommandHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly EventStoreDbContext _eventStoreDbContext;

    public DefinePartCommandHandlerTests()
    {
        var services = new ServiceCollection();

        // Set up in-memory database for event store
        var eventStoreOptions = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _eventStoreDbContext = new EventStoreDbContext(eventStoreOptions);

        // Set up in-memory database for read models
        var partsDbOptions = new DbContextOptionsBuilder<PartsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var partsDbContext = new PartsDbContext(partsDbOptions);

        // Register dependencies
        services.AddScoped(_ => _eventStoreDbContext);
        services.AddScoped(_ => partsDbContext);
        services.AddScoped<IAggregateRepository<PartAggregate>, AggregateRepository<PartAggregate>>();
        services.AddScoped<IEventStore, TestEventStore>();
        services.AddScoped<IEventBus, TestEventBus>();
        services.AddLogging();

        // Register JSON options for event serialization
        services.AddSingleton(new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IncludeFields = true,
            Converters = { new PartSkuJsonConverter(), new PartNameJsonConverter(), new QuantityJsonConverter(), new PartSourceJsonConverter() }
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var handler = new DefinePartCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>());

        var command = DefinePartCommand.Create("ABC-123", "Widget A").Value;

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the part was saved
        var repository = _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>();
        var savedPart = await repository.GetByIdAsync("ABC-123");

        Assert.True(savedPart.HasValue);
        Assert.Equal("ABC-123", savedPart.Value.Sku.Value);
        Assert.Equal("Widget A", savedPart.Value.Name.Value);
        Assert.Equal(0, (int)savedPart.Value.CurrentQuantity);
    }

    [Fact]
    public async Task Handle_WithExistingPart_ReturnsFailure()
    {
        // Arrange
        var handler = new DefinePartCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>());

        var command1 = DefinePartCommand.Create("ABC-123", "Widget A").Value;
        var command2 = DefinePartCommand.Create("ABC-123", "Widget B").Value;

        // Act
        await handler.HandleAsync(command1, CancellationToken.None);
        var result = await handler.HandleAsync(command2, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Part with SKU 'ABC-123' already exists.", result.Errors["Error"]);
    }

    // Test removed - command validation is tested separately in command creation tests

    // Test removed - command validation is tested separately in command creation tests

    public void Dispose()
    {
        _eventStoreDbContext.Dispose();
        _serviceProvider.Dispose();
    }
}

// Test implementations
public class TestEventStore(EventStoreDbContext dbContext, IEventBus eventBus, JsonSerializerOptions options)
    : EFCoreEventStore(dbContext, eventBus, new TestLogger(), options)
{
    // Use the base implementation but with test dependencies
}

public class TestLogger : ILogger<EFCoreEventStore>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Do nothing for tests
    }
}

public class TestEventBus : IEventBus
{
    public Task DispatchAsync(Event @event, CancellationToken cancellationToken = default)
    {
        // For testing, we don't need to actually dispatch events
        return Task.CompletedTask;
    }

    public Task DispatchManyAsync(IEnumerable<Event> events, CancellationToken cancellationToken = default)
    {
        // For testing, we don't need to actually dispatch events
        return Task.CompletedTask;
    }
}