using Application.Features.Product;
using Application.Features.Product.Projections;
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

namespace Application.Tests.Features.Product.Projections;

public class ProductSummaryProjectionTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly PartsDbContext _dbContext;

    public ProductSummaryProjectionTests()
    {
        var services = new ServiceCollection();

        // Set up in-memory database
        var options = new DbContextOptionsBuilder<PartsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new PartsDbContext(options);

        // Register dependencies
        services.AddScoped(_ => _dbContext);
        services.AddScoped<IEventStore, TestEventStore>();
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
                new PartSkuJsonConverter(),
                new QuantityJsonConverter()
            }
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Handle_ProductDefinedEvent_CreatesProductSummary()
    {
        // Arrange
        var projection = new ProductSummaryProjection(_dbContext);
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;
        var @event = new ProductDefinedEvent(sku, name);

        // Act
        await projection.HandleAsync(@event);

        // Assert
        var product = await _dbContext.ProductSummary.SingleOrDefaultAsync(p => p.Sku == "PROD-001");
        Assert.NotNull(product);
        Assert.Equal("PROD-001", product.Sku);
        Assert.Equal("Basic Desktop Computer", product.Name);
        Assert.Equal(0, product.PartCount);
    }

    [Fact]
    public async Task Handle_ProductDefinedEvent_UpdatesExistingProduct()
    {
        // Arrange
        var projection = new ProductSummaryProjection(_dbContext);
        var sku = ProductSku.Create("PROD-001").Value;
        var name1 = ProductName.Create("Old Name").Value;
        var name2 = ProductName.Create("New Name").Value;

        // Create initial product
        var event1 = new ProductDefinedEvent(sku, name1);
        await projection.HandleAsync(event1);

        // Update product name
        var event2 = new ProductDefinedEvent(sku, name2);

        // Act
        await projection.HandleAsync(event2);

        // Assert
        var product = await _dbContext.ProductSummary.SingleAsync(p => p.Sku == "PROD-001");
        Assert.Equal("New Name", product.Name);
        Assert.Equal(0, product.PartCount);
    }

    [Fact]
    public async Task Handle_PartAddedToProductEvent_IncrementsPartCount()
    {
        // Arrange
        var projection = new ProductSummaryProjection(_dbContext);
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;

        // Create product first
        var defineEvent = new ProductDefinedEvent(sku, name);
        await projection.HandleAsync(defineEvent);

        // Add part
        var productPart = ProductPart.Create("PART-001", 2).Value;
        var addPartEvent = new PartAddedToProductEvent(sku, productPart);

        // Act
        await projection.HandleAsync(addPartEvent);

        // Assert
        var product = await _dbContext.ProductSummary.SingleAsync(p => p.Sku == "PROD-001");
        Assert.Equal(1, product.PartCount);
    }

    [Fact]
    public async Task Handle_MultiplePartAddedEvents_IncrementsPartCountCorrectly()
    {
        // Arrange
        var projection = new ProductSummaryProjection(_dbContext);
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;

        // Create product first
        var defineEvent = new ProductDefinedEvent(sku, name);
        await projection.HandleAsync(defineEvent);

        // Add multiple parts
        var productPart1 = ProductPart.Create("PART-001", 1).Value;
        var productPart2 = ProductPart.Create("PART-002", 2).Value;
        var productPart3 = ProductPart.Create("PART-003", 1).Value;

        var addPartEvent1 = new PartAddedToProductEvent(sku, productPart1);
        var addPartEvent2 = new PartAddedToProductEvent(sku, productPart2);
        var addPartEvent3 = new PartAddedToProductEvent(sku, productPart3);

        // Act
        await projection.HandleAsync(addPartEvent1);
        await projection.HandleAsync(addPartEvent2);
        await projection.HandleAsync(addPartEvent3);

        // Assert
        var product = await _dbContext.ProductSummary.SingleAsync(p => p.Sku == "PROD-001");
        Assert.Equal(3, product.PartCount);
    }

    [Fact]
    public async Task Handle_PartAddedToNonExistentProduct_DoesNothing()
    {
        // Arrange
        var projection = new ProductSummaryProjection(_dbContext);
        var sku = ProductSku.Create("PROD-001").Value;
        var productPart = ProductPart.Create("PART-001", 2).Value;
        var addPartEvent = new PartAddedToProductEvent(sku, productPart);

        // Act
        await projection.HandleAsync(addPartEvent);

        // Assert
        var product = await _dbContext.ProductSummary.SingleOrDefaultAsync(p => p.Sku == "PROD-001");
        Assert.Null(product);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
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