using Application.Features.Part;
using Application.Features.Part.Projections;
using Application.Features.Part.ValueObjects;
using Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Features.Part.Projections;

public class PartSummaryProjectionTests : IDisposable
{
    private readonly PartsDbContext _dbContext;
    private readonly PartSummaryProjection _projection;

    public PartSummaryProjectionTests()
    {
        var options = new DbContextOptionsBuilder<PartsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PartsDbContext(options);
        _projection = new PartSummaryProjection(_dbContext);
    }

    [Fact]
    public async Task HandleAsync_PartDefinedEvent_CreatesPartSummary()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var @event = new PartDefinedEvent(sku, name);

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.NotNull(partSummary);
        Assert.Equal("ABC-123", partSummary.Sku);
        Assert.Equal("Widget A", partSummary.Name);
        Assert.Equal(0, partSummary.Quantity);
        Assert.Equal("", partSummary.SourceUri);
        Assert.Equal("", partSummary.SourceName);
    }

    [Fact]
    public async Task HandleAsync_PartDefinedEvent_UpdatesExistingPartSummary()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var @event = new PartDefinedEvent(sku, name);

        // Pre-create a part summary
        _dbContext.PartSummary.Add(new PartSummary { Sku = "ABC-123", Name = "Old Name", Quantity = 10 });
        await _dbContext.SaveChangesAsync();

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.NotNull(partSummary);
        Assert.Equal("ABC-123", partSummary.Sku);
        Assert.Equal("Widget A", partSummary.Name); // Should be updated
        Assert.Equal(10, partSummary.Quantity); // Should remain unchanged
    }

    [Fact]
    public async Task HandleAsync_PartAcquiredEvent_IncreasesQuantity()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var quantity = Quantity.Create(50).Value;
        var @event = new PartAcquiredEvent(sku, quantity, "Initial stock");

        // Pre-create a part summary
        _dbContext.PartSummary.Add(new PartSummary { Sku = "ABC-123", Name = "Widget A", Quantity = 10 });
        await _dbContext.SaveChangesAsync();

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.NotNull(partSummary);
        Assert.Equal(60, partSummary.Quantity); // 10 + 50
    }

    [Fact]
    public async Task HandleAsync_PartAcquiredEvent_IgnoresNonExistentPart()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var quantity = Quantity.Create(50).Value;
        var @event = new PartAcquiredEvent(sku, quantity, "Initial stock");

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.Null(partSummary);
    }

    [Fact]
    public async Task HandleAsync_PartConsumedEvent_DecreasesQuantity()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var quantity = Quantity.Create(30).Value;
        var @event = new PartConsumedEvent(sku, quantity, "Used in production");

        // Pre-create a part summary
        _dbContext.PartSummary.Add(new PartSummary { Sku = "ABC-123", Name = "Widget A", Quantity = 100 });
        await _dbContext.SaveChangesAsync();

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.NotNull(partSummary);
        Assert.Equal(70, partSummary.Quantity); // 100 - 30
    }

    [Fact]
    public async Task HandleAsync_PartConsumedEvent_IgnoresNonExistentPart()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var quantity = Quantity.Create(30).Value;
        var @event = new PartConsumedEvent(sku, quantity, "Used in production");

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.Null(partSummary);
    }

    [Fact]
    public async Task HandleAsync_PartRecountedEvent_SetsQuantity()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var quantity = Quantity.Create(75).Value;
        var @event = new PartRecountedEvent(sku, quantity, "Physical count");

        // Pre-create a part summary
        _dbContext.PartSummary.Add(new PartSummary { Sku = "ABC-123", Name = "Widget A", Quantity = 100 });
        await _dbContext.SaveChangesAsync();

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.NotNull(partSummary);
        Assert.Equal(75, partSummary.Quantity); // Should be set to exactly 75
    }

    [Fact]
    public async Task HandleAsync_PartRecountedEvent_IgnoresNonExistentPart()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var quantity = Quantity.Create(75).Value;
        var @event = new PartRecountedEvent(sku, quantity, "Physical count");

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.Null(partSummary);
    }

    [Fact]
    public async Task HandleAsync_PartSourceUpdatedEvent_UpdatesSource()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var source = PartSource.Create("Supplier A", "https://supplier-a.com").Value;
        var @event = new PartSourceUpdatedEvent(sku, source);

        // Pre-create a part summary
        _dbContext.PartSummary.Add(new PartSummary { Sku = "ABC-123", Name = "Widget A", Quantity = 50 });
        await _dbContext.SaveChangesAsync();

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.NotNull(partSummary);
        Assert.Equal("Supplier A", partSummary.SourceName);
        Assert.Equal("https://supplier-a.com", partSummary.SourceUri);
    }

    [Fact]
    public async Task HandleAsync_PartSourceUpdatedEvent_IgnoresNonExistentPart()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var source = PartSource.Create("Supplier A", "https://supplier-a.com").Value;
        var @event = new PartSourceUpdatedEvent(sku, source);

        // Act
        await _projection.HandleAsync(@event);

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.Null(partSummary);
    }

    [Fact]
    public async Task HandleAsync_MultipleEvents_UpdatesPartSummaryCorrectly()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var source = PartSource.Create("Supplier A", "https://supplier-a.com").Value;

        var events = new Event[]
        {
            new PartDefinedEvent(sku, name),
            new PartAcquiredEvent(sku, Quantity.Create(100).Value, "Initial stock"),
            new PartConsumedEvent(sku, Quantity.Create(20).Value, "Used in production"),
            new PartAcquiredEvent(sku, Quantity.Create(50).Value, "Additional stock"),
            new PartSourceUpdatedEvent(sku, source),
            new PartRecountedEvent(sku, Quantity.Create(140).Value, "Physical count")
        };

        // Act
        foreach (var @event in events)
        {
            switch (@event)
            {
                case PartDefinedEvent e:
                    await _projection.HandleAsync(e);
                    break;
                case PartAcquiredEvent e:
                    await _projection.HandleAsync(e);
                    break;
                case PartConsumedEvent e:
                    await _projection.HandleAsync(e);
                    break;
                case PartSourceUpdatedEvent e:
                    await _projection.HandleAsync(e);
                    break;
                case PartRecountedEvent e:
                    await _projection.HandleAsync(e);
                    break;
            }
        }

        // Assert
        var partSummary = await _dbContext.PartSummary.SingleOrDefaultAsync(p => p.Sku == "ABC-123");
        Assert.NotNull(partSummary);
        Assert.Equal("ABC-123", partSummary.Sku);
        Assert.Equal("Widget A", partSummary.Name);
        Assert.Equal(140, partSummary.Quantity); // Final recount value
        Assert.Equal("Supplier A", partSummary.SourceName);
        Assert.Equal("https://supplier-a.com", partSummary.SourceUri);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}