using Application.Features.Product.Queries;
using Application.Features.Product.Projections;
using Application;
using Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Features.Product.Queries;

public class GetAllProductsQueryHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly PartsDbContext _dbContext;

    public GetAllProductsQueryHandlerTests()
    {
        var services = new ServiceCollection();

        // Set up in-memory database
        var options = new DbContextOptionsBuilder<PartsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new PartsDbContext(options);
        services.AddScoped(_ => _dbContext);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Handle_WithNoProducts_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetAllProductsQueryHandler(_dbContext);
        var query = new GetAllProductsQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_WithProducts_ReturnsAllProductsOrderedBySku()
    {
        // Arrange
        var handler = new GetAllProductsQueryHandler(_dbContext);
        var query = new GetAllProductsQuery();

        // Add products in non-alphabetical order
        await _dbContext.ProductSummary.AddRangeAsync(
            new ProductSummaryReadModel { Sku = "PROD-003", Name = "Third Product", PartCount = 2 },
            new ProductSummaryReadModel { Sku = "PROD-001", Name = "First Product", PartCount = 1 },
            new ProductSummaryReadModel { Sku = "PROD-002", Name = "Second Product", PartCount = 3 }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);

        // Verify ordering by SKU
        Assert.Equal("PROD-001", result.Value[0].Sku);
        Assert.Equal("First Product", result.Value[0].Name);
        Assert.Equal(1, result.Value[0].PartCount);

        Assert.Equal("PROD-002", result.Value[1].Sku);
        Assert.Equal("Second Product", result.Value[1].Name);
        Assert.Equal(3, result.Value[1].PartCount);

        Assert.Equal("PROD-003", result.Value[2].Sku);
        Assert.Equal("Third Product", result.Value[2].Name);
        Assert.Equal(2, result.Value[2].PartCount);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}

public class GetProductBySkuQueryHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly PartsDbContext _dbContext;

    public GetProductBySkuQueryHandlerTests()
    {
        var services = new ServiceCollection();

        // Set up in-memory database
        var options = new DbContextOptionsBuilder<PartsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new PartsDbContext(options);
        services.AddScoped(_ => _dbContext);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Handle_WithValidSku_ReturnsProductWithTransactions()
    {
        // Arrange
        var handler = new GetProductBySkuQueryHandler(_dbContext);
        var query = GetProductBySkuQuery.Create("PROD-001").Value;

        // Create product with transactions
        var product = new ProductDetailReadModel
        {
            Sku = "PROD-001",
            Name = "Test Product",
            PartCount = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastModified = DateTime.UtcNow,
            PartTransactions = new List<ProductPartTransactionReadModel>
            {
                new() { ProductSku = "PROD-001", PartSku = "PART-002", Type = "PART_ADDED", Quantity = 1, Timestamp = DateTime.UtcNow },
                new() { ProductSku = "PROD-001", PartSku = "PART-001", Type = "PART_ADDED", Quantity = 2, Timestamp = DateTime.UtcNow.AddMinutes(-5) }
            }
        };

        await _dbContext.ProductDetails.AddAsync(product);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("PROD-001", result.Value.Sku);
        Assert.Equal("Test Product", result.Value.Name);
        Assert.Equal(2, result.Value.PartCount);

        // Verify transactions are ordered by timestamp (most recent first)
        Assert.Equal(2, result.Value.PartTransactions.Count);
        Assert.Equal("PART-002", result.Value.PartTransactions[0].PartSku);
        Assert.Equal("PART-001", result.Value.PartTransactions[1].PartSku);
    }

    [Fact]
    public async Task Handle_WithNonExistentSku_ReturnsNull()
    {
        // Arrange
        var handler = new GetProductBySkuQueryHandler(_dbContext);
        var query = GetProductBySkuQuery.Create("NONEXISTENT").Value;

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Create_WithEmptySku_ReturnsFailure()
    {
        // Act
        var result = GetProductBySkuQuery.Create("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("SKU cannot be empty", result.Errors["sku"]);
    }

    [Fact]
    public async Task Create_WithWhitespaceSku_ReturnsFailure()
    {
        // Act
        var result = GetProductBySkuQuery.Create("   ");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("SKU cannot be empty", result.Errors["sku"]);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}