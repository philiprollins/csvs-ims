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

public class AcquirePartCommandHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly EventStoreDbContext _eventStoreDbContext;

    public AcquirePartCommandHandlerTests()
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
    public async Task Handle_WithValidCommand_IncreasesQuantity()
    {
        // Arrange
        var defineHandler = new DefinePartCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>());
        var acquireHandler = new AcquirePartCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>());

        var defineCommand = DefinePartCommand.Create("ABC-123", "Widget A").Value;
        var acquireCommand = AcquirePartCommand.Create("ABC-123", 10, "Initial stock").Value;

        // Act
        await defineHandler.HandleAsync(defineCommand, CancellationToken.None);
        var result = await acquireHandler.HandleAsync(acquireCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the quantity was updated
        var repository = _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>();
        var savedPart = await repository.GetByIdAsync("ABC-123");

        Assert.True(savedPart.HasValue);
        Assert.Equal(10, (int)savedPart.Value.CurrentQuantity);
    }

    [Fact]
    public async Task Handle_WithNonExistentPart_ReturnsFailure()
    {
        // Arrange
        var handler = new AcquirePartCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>());

        var command = AcquirePartCommand.Create("NON-EXISTENT", 10, "Test").Value;

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Part with SKU 'NON-EXISTENT' does not exist.", result.Errors["Error"]);
    }

    [Fact]
    public void Create_WithEmptyJustification_ReturnsFailure()
    {
        // Act
        var result = AcquirePartCommand.Create("ABC-123", 10, "");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Justification is required", result.Errors["justification"]);
    }

    public void Dispose()
    {
        _eventStoreDbContext.Dispose();
        _serviceProvider.Dispose();
    }
}