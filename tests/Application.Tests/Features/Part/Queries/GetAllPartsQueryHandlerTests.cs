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

public class GetAllPartsQueryHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly PartsDbContext _partsDbContext;

    public GetAllPartsQueryHandlerTests()
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
    public async Task Handle_WithNoParts_ReturnsEmptyResult()
    {
        // Arrange
        var handler = new GetAllPartsQueryHandler(_partsDbContext);
        var query = GetAllPartsQuery.Create(1, 10);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(1, result.Meta.Page);
        Assert.Equal(10, result.Meta.PageSize);
        Assert.Equal(0, result.Meta.TotalItems);
        Assert.Equal(0, result.Meta.TotalPages);
    }

    [Fact]
    public async Task Handle_WithParts_ReturnsPaginatedResult()
    {
        // Arrange
        await SetupTestData();
        var handler = new GetAllPartsQueryHandler(_partsDbContext);
        var query = GetAllPartsQuery.Create(1, 10);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Meta.Page);
        Assert.Equal(10, result.Meta.PageSize);
        Assert.Equal(2, result.Meta.TotalItems);
        Assert.Equal(1, result.Meta.TotalPages);

        // Check first part
        var firstPart = result.Items.First();
        Assert.Equal("ABC-123", firstPart.Sku);
        Assert.Equal("Widget A", firstPart.Name);
        Assert.Equal(10, firstPart.Quantity);
        Assert.Equal("Supplier Inc", firstPart.SourceName);
        Assert.Equal("https://supplier.com", firstPart.SourceUri);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await SetupTestData();
        var handler = new GetAllPartsQueryHandler(_partsDbContext);
        var query = GetAllPartsQuery.Create(1, 1); // Page 1, size 1

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(1, result.Meta.Page);
        Assert.Equal(1, result.Meta.PageSize);
        Assert.Equal(2, result.Meta.TotalItems);
        Assert.Equal(2, result.Meta.TotalPages);

        // Should return first part alphabetically
        var part = result.Items.First();
        Assert.Equal("ABC-123", part.Sku);
    }

    [Fact]
    public async Task Handle_WithInvalidPage_UsesDefaultValues()
    {
        // Arrange
        await SetupTestData();
        var handler = new GetAllPartsQueryHandler(_partsDbContext);
        var query = GetAllPartsQuery.Create(0, 0); // Invalid values

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Meta.Page); // Should default to 1
        Assert.Equal(20, result.Meta.PageSize); // Should default to 20
    }

    private async Task SetupTestData()
    {
        // Clear existing data
        _partsDbContext.PartSummary.RemoveRange(_partsDbContext.PartSummary);
        await _partsDbContext.SaveChangesAsync();

        // Add test parts
        var part1 = new PartSummary
        {
            Sku = "ABC-123",
            Name = "Widget A",
            Quantity = 10,
            SourceName = "Supplier Inc",
            SourceUri = "https://supplier.com"
        };

        var part2 = new PartSummary
        {
            Sku = "XYZ-789",
            Name = "Widget B",
            Quantity = 5,
            SourceName = "",
            SourceUri = ""
        };

        _partsDbContext.PartSummary.AddRange(part1, part2);
        await _partsDbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _partsDbContext.Dispose();
        _serviceProvider.Dispose();
    }
}

// Test implementations
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