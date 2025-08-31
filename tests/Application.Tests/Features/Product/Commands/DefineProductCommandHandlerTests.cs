using Application.Features.Product;
using Application.Features.Product.Commands;
using Application.Features.Product.ValueObjects;
using Application.Features.Part.ValueObjects;
using Application;
using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Tests.Features.Product.Commands;

public class DefineProductCommandHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly EventStoreDbContext _eventStoreDbContext;

    public DefineProductCommandHandlerTests()
    {
        var services = new ServiceCollection();

        // Set up in-memory database for event store
        var eventStoreOptions = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _eventStoreDbContext = new EventStoreDbContext(eventStoreOptions);

        // Register dependencies
        services.AddScoped(_ => _eventStoreDbContext);
        services.AddScoped<IAggregateRepository<ProductAggregate>, AggregateRepository<ProductAggregate>>();
        services.AddScoped<IEventStore>(sp => new TestEventStore(
            sp.GetRequiredService<EventStoreDbContext>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<ILogger<EFCoreEventStore>>(),
            sp.GetRequiredService<JsonSerializerOptions>()));
        services.AddScoped<IEventBus, TestEventBus>();
        services.AddLogging();

        // Register JSON options for event serialization
        services.AddSingleton(new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IncludeFields = true,
            Converters = { 
                new ProductSkuJsonConverter(), 
                new ProductNameJsonConverter(), 
                new ProductPartJsonConverter(),
                new PartSkuJsonConverter(),
                new QuantityJsonConverter()
            }
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var handler = new DefineProductCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<ProductAggregate>>());

        var command = DefineProductCommand.Create("PROD-001", "Basic Desktop Computer").Value;

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the product was saved
        var repository = _serviceProvider.GetRequiredService<IAggregateRepository<ProductAggregate>>();
        var savedProduct = await repository.GetByIdAsync("PROD-001");

        Assert.True(savedProduct.HasValue);
        Assert.Equal("PROD-001", savedProduct.Value.Sku.Value);
        Assert.Equal("Basic Desktop Computer", savedProduct.Value.Name.Value);
        Assert.Empty(savedProduct.Value.Parts);
    }

    [Fact]
    public async Task Handle_WithExistingProduct_ReturnsFailure()
    {
        // Arrange
        var handler = new DefineProductCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<ProductAggregate>>());

        var command1 = DefineProductCommand.Create("PROD-001", "First Product").Value;
        var command2 = DefineProductCommand.Create("PROD-001", "Second Product").Value;

        // Act
        await handler.HandleAsync(command1, CancellationToken.None);
        var result = await handler.HandleAsync(command2, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Product with SKU 'PROD-001' already exists.", result.Errors["Error"]);
    }

    public void Dispose()
    {
        _eventStoreDbContext.Dispose();
        _serviceProvider.Dispose();
    }
}

// Test implementations
public class TestEventStore(EventStoreDbContext dbContext, IEventBus eventBus, ILogger<EFCoreEventStore> logger, JsonSerializerOptions options)
    : EFCoreEventStore(dbContext, eventBus, logger, options)
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