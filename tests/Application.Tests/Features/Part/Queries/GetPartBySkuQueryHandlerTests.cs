using Application.Features.Part;
using Application.Features.Part.Commands;
using Application.Features.Part.Projections;
using Application.Features.Part.Queries;
using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Tests.Features.Part.Queries;

public class GetPartBySkuQueryHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly PartsDbContext _partsDbContext;

    public GetPartBySkuQueryHandlerTests()
    {
        var services = new ServiceCollection();

        // Set up in-memory database for read models
        var partsDbOptions = new DbContextOptionsBuilder<PartsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _partsDbContext = new PartsDbContext(partsDbOptions);

        // Register dependencies
        services.AddScoped(_ => _partsDbContext);
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
    public async Task Handle_WithNonExistentPart_ReturnsFailure()
    {
        // Arrange
        var handler = new GetPartBySkuQueryHandler(_partsDbContext);
        var query = GetPartBySkuQuery.Create("NON-EXISTENT").Value;

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Part with SKU 'NON-EXISTENT' not found", result.Errors["Error"]);
    }

    [Fact]
    public async Task Handle_WithExistingPart_ReturnsPartDetails()
    {
        // Arrange
        await SetupTestData();
        var handler = new GetPartBySkuQueryHandler(_partsDbContext);
        var query = GetPartBySkuQuery.Create("ABC-123").Value;

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var part = result.Value;

        Assert.Equal("ABC-123", part.Sku);
        Assert.Equal("Widget A", part.Name);
        Assert.Equal(10, part.Quantity);
        Assert.Equal("Supplier Inc", part.SourceName);
        Assert.Equal("https://supplier.com", part.SourceUri);
    }

    [Fact]
    public async Task Handle_WithPartWithTransactions_ReturnsTransactionHistory()
    {
        // Arrange
        await SetupTestDataWithTransactions();
        var handler = new GetPartBySkuQueryHandler(_partsDbContext);
        var query = GetPartBySkuQuery.Create("ABC-123").Value;

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var part = result.Value;

        Assert.Equal(2, part.Transactions.Count);

        // Transactions should be ordered by timestamp descending
        var firstTransaction = part.Transactions[0];
        Assert.Equal("CONSUMED", firstTransaction.Type);
        Assert.Equal(-5, firstTransaction.Quantity);
        Assert.Equal("Used in production", firstTransaction.Justification);

        var secondTransaction = part.Transactions[1];
        Assert.Equal("ACQUIRED", secondTransaction.Type);
        Assert.Equal(15, secondTransaction.Quantity);
        Assert.Equal("Initial stock", secondTransaction.Justification);
    }

    [Fact]
    public async Task Handle_WithPartWithoutSource_ReturnsNullSourceFields()
    {
        // Arrange
        await SetupTestData();
        var handler = new GetPartBySkuQueryHandler(_partsDbContext);
        var query = GetPartBySkuQuery.Create("XYZ-789").Value;

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var part = result.Value;

        Assert.Equal("XYZ-789", part.Sku);
        Assert.Equal("Widget B", part.Name);
        Assert.Equal(5, part.Quantity);
        Assert.Null(part.SourceName);
        Assert.Null(part.SourceUri);
    }

    [Fact]
    public void Create_WithInvalidSku_ReturnsFailure()
    {
        // Act
        var result = GetPartBySkuQuery.Create("");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("SKU cannot be empty", result.Errors["sku"]);
    }

    private async Task SetupTestData()
    {
        // Clear existing data
        _partsDbContext.PartDetails.RemoveRange(_partsDbContext.PartDetails);
        _partsDbContext.PartTransactions.RemoveRange(_partsDbContext.PartTransactions);
        await _partsDbContext.SaveChangesAsync();

        // Add test parts
        var part1 = new PartDetail
        {
            Sku = "ABC-123",
            Name = "Widget A",
            Quantity = 10,
            SourceName = "Supplier Inc",
            SourceUri = "https://supplier.com",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastModified = DateTime.UtcNow,
            Transactions = new List<PartTransaction>()
        };

        var part2 = new PartDetail
        {
            Sku = "XYZ-789",
            Name = "Widget B",
            Quantity = 5,
            SourceName = "",
            SourceUri = "",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastModified = DateTime.UtcNow,
            Transactions = new List<PartTransaction>()
        };

        _partsDbContext.PartDetails.AddRange(part1, part2);
        await _partsDbContext.SaveChangesAsync();
    }

    private async Task SetupTestDataWithTransactions()
    {
        // Clear existing data
        _partsDbContext.PartDetails.RemoveRange(_partsDbContext.PartDetails);
        _partsDbContext.PartTransactions.RemoveRange(_partsDbContext.PartTransactions);
        await _partsDbContext.SaveChangesAsync();

        // Create part
        var part = new PartDetail
        {
            Sku = "ABC-123",
            Name = "Widget A",
            Quantity = 10,
            SourceName = "Supplier Inc",
            SourceUri = "https://supplier.com",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            LastModified = DateTime.UtcNow,
            Transactions = new List<PartTransaction>()
        };

        _partsDbContext.PartDetails.Add(part);
        await _partsDbContext.SaveChangesAsync();

        // Add transactions
        var transaction1 = new PartTransaction
        {
            PartSku = "ABC-123",
            Type = "ACQUIRED",
            Quantity = 15,
            QuantityBefore = 0,
            QuantityAfter = 15,
            Justification = "Initial stock",
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Part = part
        };

        var transaction2 = new PartTransaction
        {
            PartSku = "ABC-123",
            Type = "CONSUMED",
            Quantity = -5,
            QuantityBefore = 15,
            QuantityAfter = 10,
            Justification = "Used in production",
            Timestamp = DateTime.UtcNow,
            Part = part
        };

        _partsDbContext.PartTransactions.AddRange(transaction1, transaction2);
        await _partsDbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _partsDbContext.Dispose();
        _serviceProvider.Dispose();
    }
}