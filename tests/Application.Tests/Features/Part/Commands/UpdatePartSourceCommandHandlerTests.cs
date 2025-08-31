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

public class UpdatePartSourceCommandHandlerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly EventStoreDbContext _eventStoreDbContext;

    public UpdatePartSourceCommandHandlerTests()
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
    public async Task Handle_WithValidCommand_UpdatesSource()
    {
        // Arrange
        var defineHandler = new DefinePartCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>());
        var updateHandler = new UpdatePartSourceCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>());

        var defineCommand = DefinePartCommand.Create("ABC-123", "Widget A").Value;
        var updateCommand = UpdatePartSourceCommand.Create("ABC-123", "Supplier Inc", "https://supplier.com").Value;

        // Act
        await defineHandler.HandleAsync(defineCommand, CancellationToken.None);
        var result = await updateHandler.HandleAsync(updateCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the source was updated
        var repository = _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>();
        var savedPart = await repository.GetByIdAsync("ABC-123");

        Assert.True(savedPart.HasValue);
        Assert.True(savedPart.Value.Source.HasValue);
        Assert.Equal("Supplier Inc", savedPart.Value.Source.Value.Name);
        Assert.Equal("https://supplier.com", savedPart.Value.Source.Value.Uri);
    }

    [Fact]
    public async Task Handle_WithNonExistentPart_ReturnsFailure()
    {
        // Arrange
        var handler = new UpdatePartSourceCommandHandler(
            _serviceProvider.GetRequiredService<IAggregateRepository<PartAggregate>>());

        var command = UpdatePartSourceCommand.Create("NON-EXISTENT", "Supplier", "https://supplier.com").Value;

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Part with SKU 'NON-EXISTENT' does not exist.", result.Errors["Error"]);
    }

    [Fact]
    public void Create_WithInvalidSourceName_ReturnsFailure()
    {
        // Act
        var result = UpdatePartSourceCommand.Create("ABC-123", "", "https://supplier.com");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Source name cannot be empty", result.Errors["sourceName"]);
    }

    [Fact]
    public void Create_WithInvalidSourceUri_ReturnsFailure()
    {
        // Act
        var result = UpdatePartSourceCommand.Create("ABC-123", "Supplier", "invalid-uri");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Source URI is not a valid absolute URI", result.Errors["sourceUri"]);
    }

    public void Dispose()
    {
        _eventStoreDbContext.Dispose();
        _serviceProvider.Dispose();
    }
}