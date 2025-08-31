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

public class AddPartToProductCommandHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly EventStoreDbContext _eventStoreDbContext;

    public AddPartToProductCommandHandlerTests()
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
        var repository = _serviceProvider.GetRequiredService<IAggregateRepository<ProductAggregate>>();
        var handler = new AddPartToProductCommandHandler(repository);

        // Create and save a product first
        var defineCommand = DefineProductCommand.Create("PROD-001", "Basic Desktop Computer").Value;
        var defineHandler = new DefineProductCommandHandler(repository);
        await defineHandler.HandleAsync(defineCommand, CancellationToken.None);

        var command = AddPartToProductCommand.Create("PROD-001", "PART-001", 2).Value;

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the part was added to the product
        var savedProduct = await repository.GetByIdAsync("PROD-001");
        Assert.True(savedProduct.HasValue);
        Assert.Single(savedProduct.Value.Parts);
        Assert.Equal("PART-001", savedProduct.Value.Parts[0].PartSku.Value);
        Assert.Equal(2, (int)savedProduct.Value.Parts[0].Quantity);
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ReturnsFailure()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<IAggregateRepository<ProductAggregate>>();
        var handler = new AddPartToProductCommandHandler(repository);

        var command = AddPartToProductCommand.Create("NONEXISTENT", "PART-001", 2).Value;

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Product with SKU 'NONEXISTENT' does not exist.", result.Errors["Error"]);
    }

    [Fact]
    public async Task Handle_WithDuplicatePart_ReturnsFailure()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<IAggregateRepository<ProductAggregate>>();
        var handler = new AddPartToProductCommandHandler(repository);

        // Create and save a product first
        var defineCommand = DefineProductCommand.Create("PROD-001", "Basic Desktop Computer").Value;
        var defineHandler = new DefineProductCommandHandler(repository);
        await defineHandler.HandleAsync(defineCommand, CancellationToken.None);

        // Add a part
        var command1 = AddPartToProductCommand.Create("PROD-001", "PART-001", 2).Value;
        await handler.HandleAsync(command1, CancellationToken.None);

        // Try to add the same part again
        var command2 = AddPartToProductCommand.Create("PROD-001", "PART-001", 3).Value;

        // Act
        var result = await handler.HandleAsync(command2, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("partSku", result.Errors);
        Assert.Equal("Part 'PART-001' is already added to this product", result.Errors["partSku"]);
    }

    [Fact]
    public async Task Handle_WithMultipleParts_ReturnsSuccess()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<IAggregateRepository<ProductAggregate>>();
        var handler = new AddPartToProductCommandHandler(repository);

        // Create and save a product first
        var defineCommand = DefineProductCommand.Create("PROD-001", "Basic Desktop Computer").Value;
        var defineHandler = new DefineProductCommandHandler(repository);
        await defineHandler.HandleAsync(defineCommand, CancellationToken.None);

        var command1 = AddPartToProductCommand.Create("PROD-001", "PART-001", 1).Value;
        var command2 = AddPartToProductCommand.Create("PROD-001", "PART-002", 2).Value;
        var command3 = AddPartToProductCommand.Create("PROD-001", "PART-003", 1).Value;

        // Act
        await handler.HandleAsync(command1, CancellationToken.None);
        await handler.HandleAsync(command2, CancellationToken.None);
        var result = await handler.HandleAsync(command3, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify all parts were added to the product
        var savedProduct = await repository.GetByIdAsync("PROD-001");
        Assert.True(savedProduct.HasValue);
        Assert.Equal(3, savedProduct.Value.Parts.Count);

        // Verify all parts are present
        Assert.Contains(savedProduct.Value.Parts, p => p.PartSku.Value == "PART-001" && p.Quantity.Value == 1);
        Assert.Contains(savedProduct.Value.Parts, p => p.PartSku.Value == "PART-002" && p.Quantity.Value == 2);
        Assert.Contains(savedProduct.Value.Parts, p => p.PartSku.Value == "PART-003" && p.Quantity.Value == 1);
    }

    public void Dispose()
    {
        _eventStoreDbContext.Dispose();
        _serviceProvider.Dispose();
    }
}