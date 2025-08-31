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

public class ProductDetailProjectionTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly PartsDbContext _dbContext;

    public ProductDetailProjectionTests()
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
    public async Task Handle_ProductDefinedEvent_CreatesProductDetail()
    {
        // Arrange
        var projection = new ProductDetailProjection(_dbContext);
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;
        var @event = new ProductDefinedEvent(sku, name);

        // Act
        await projection.HandleAsync(@event);

        // Assert
        var product = await _dbContext.ProductDetails.SingleOrDefaultAsync(p => p.Sku == "PROD-001");
        Assert.NotNull(product);
        Assert.Equal("PROD-001", product.Sku);
        Assert.Equal("Basic Desktop Computer", product.Name);
        Assert.Equal(0, product.PartCount);
        Assert.Equal(@event.Timestamp, product.CreatedAt);
        Assert.Equal(@event.Timestamp, product.LastModified);
        Assert.Empty(product.PartTransactions);
    }

    [Fact]
    public async Task Handle_ProductDefinedEvent_DoesNotDuplicateExistingProduct()
    {
        // Arrange
        var projection = new ProductDetailProjection(_dbContext);
        var sku = ProductSku.Create("PROD-001").Value;
        var name1 = ProductName.Create("Old Name").Value;
        var name2 = ProductName.Create("New Name").Value;

        // Create initial product
        var event1 = new ProductDefinedEvent(sku, name1);
        await projection.HandleAsync(event1);

        // Try to create again with different name
        var event2 = new ProductDefinedEvent(sku, name2);

        // Act
        await projection.HandleAsync(event2);

        // Assert
        var products = await _dbContext.ProductDetails.Where(p => p.Sku == "PROD-001").ToListAsync();
        Assert.Single(products);
        var product = products[0];
        Assert.Equal("Old Name", product.Name); // Should keep original name
    }

    [Fact]
    public async Task Handle_PartAddedToProductEvent_IncrementsPartCountAndCreatesTransaction()
    {
        // Arrange
        var projection = new ProductDetailProjection(_dbContext);
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
        var product = await _dbContext.ProductDetails
            .Include(p => p.PartTransactions)
            .SingleAsync(p => p.Sku == "PROD-001");

        Assert.Equal(1, product.PartCount);
        Assert.Equal(addPartEvent.Timestamp, product.LastModified);
        Assert.Single(product.PartTransactions);

        var transaction = product.PartTransactions[0];
        Assert.Equal("PROD-001", transaction.ProductSku);
        Assert.Equal("PART-001", transaction.PartSku);
        Assert.Equal("PART_ADDED", transaction.Type);
        Assert.Equal(2, transaction.Quantity);
        Assert.Equal(addPartEvent.Timestamp, transaction.Timestamp);
    }

    [Fact]
    public async Task Handle_MultiplePartAddedEvents_CreatesMultipleTransactions()
    {
        // Arrange
        var projection = new ProductDetailProjection(_dbContext);
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;

        // Create product first
        var defineEvent = new ProductDefinedEvent(sku, name);
        await projection.HandleAsync(defineEvent);

        // Add multiple parts
        var productPart1 = ProductPart.Create("PART-001", 1).Value;
        var productPart2 = ProductPart.Create("PART-002", 2).Value;

        var addPartEvent1 = new PartAddedToProductEvent(sku, productPart1);
        var addPartEvent2 = new PartAddedToProductEvent(sku, productPart2);

        // Act
        await projection.HandleAsync(addPartEvent1);
        await projection.HandleAsync(addPartEvent2);

        // Assert
        var product = await _dbContext.ProductDetails
            .Include(p => p.PartTransactions)
            .SingleAsync(p => p.Sku == "PROD-001");

        Assert.Equal(2, product.PartCount);
        Assert.Equal(2, product.PartTransactions.Count);

        // Verify transactions are ordered by timestamp (most recent first)
        var transactions = product.PartTransactions.OrderByDescending(t => t.Timestamp).ToList();
        Assert.Equal("PART-002", transactions[0].PartSku);
        Assert.Equal("PART-001", transactions[1].PartSku);
    }

    [Fact]
    public async Task Handle_PartAddedToNonExistentProduct_DoesNothing()
    {
        // Arrange
        var projection = new ProductDetailProjection(_dbContext);
        var sku = ProductSku.Create("PROD-001").Value;
        var productPart = ProductPart.Create("PART-001", 2).Value;
        var addPartEvent = new PartAddedToProductEvent(sku, productPart);

        // Act
        await projection.HandleAsync(addPartEvent);

        // Assert
        var product = await _dbContext.ProductDetails.SingleOrDefaultAsync(p => p.Sku == "PROD-001");
        Assert.Null(product);

        var transactions = await _dbContext.ProductPartTransactions.ToListAsync();
        Assert.Empty(transactions);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}