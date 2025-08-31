using Application.Features.Part;
using Application.Features.Part.ValueObjects;
using Library;

namespace Application.Tests.Features.Part;

public class PartAggregateTests
{
    [Fact]
    public void Define_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;

        // Act
        var result = PartAggregate.Define(sku, name);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ABC-123", result.Value.Sku.Value);
        Assert.Equal("Widget A", result.Value.Name.Value);
        Assert.Equal(0, (int)result.Value.CurrentQuantity);
        Assert.True(result.Value.Source.IsNone);

        var uncommittedChanges = result.Value.GetUncommittedChanges();
        Assert.Single(uncommittedChanges);
        Assert.IsType<PartDefinedEvent>(uncommittedChanges[0]);
        var definedEvent = (PartDefinedEvent)uncommittedChanges[0];
        Assert.Equal("ABC-123", definedEvent.Sku.Value);
        Assert.Equal("Widget A", definedEvent.Name.Value);
    }

    [Fact]
    public void Acquire_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        var quantity = Quantity.Create(50).Value;
        var justification = "Initial stock";

        // Act
        var result = part.Acquire(quantity, justification);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, (int)result.Value.CurrentQuantity);

        var uncommittedChanges = result.Value.GetUncommittedChanges();
        Assert.Single(uncommittedChanges);
        Assert.IsType<PartAcquiredEvent>(uncommittedChanges[0]);
        var acquiredEvent = (PartAcquiredEvent)uncommittedChanges[0];
        Assert.Equal("ABC-123", acquiredEvent.Sku.Value);
        Assert.Equal(50, (int)acquiredEvent.Quantity);
        Assert.Equal("Initial stock", acquiredEvent.Justification);
    }

    [Fact]
    public void Acquire_WithEmptyJustification_ReturnsFailure()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        var quantity = Quantity.Create(50).Value;
        var emptyJustification = "";

        // Act
        var result = part.Acquire(quantity, emptyJustification);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("justification", result.Errors);
        Assert.Equal("Justification is required for acquiring parts", result.Errors["justification"]);
    }

    [Fact]
    public void Acquire_WithWhitespaceJustification_ReturnsFailure()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        var quantity = Quantity.Create(50).Value;
        var whitespaceJustification = "   ";

        // Act
        var result = part.Acquire(quantity, whitespaceJustification);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("justification", result.Errors);
        Assert.Equal("Justification is required for acquiring parts", result.Errors["justification"]);
    }

    [Fact]
    public void Acquire_WithQuantityThatExceedsMax_ReturnsFailure()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        // First acquire some quantity
        var initialQuantity = Quantity.Create(999998).Value;
        part = part.Acquire(initialQuantity, "Initial stock").Value;
        part.MarkChangesAsCommitted();

        var largeQuantity = Quantity.Create(2).Value; // This will exceed the max
        var justification = "Large acquisition";

        // Act
        var result = part.Acquire(largeQuantity, justification);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("quantity", result.Errors);
        Assert.Equal("Quantity exceeds maximum allowed (999,999)", result.Errors["quantity"]);
    }

    [Fact]
    public void Consume_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        // First acquire some parts
        var acquireQuantity = Quantity.Create(100).Value;
        part = part.Acquire(acquireQuantity, "Initial stock").Value;
        part.MarkChangesAsCommitted();

        var consumeQuantity = Quantity.Create(30).Value;
        var justification = "Used in production";

        // Act
        var result = part.Consume(consumeQuantity, justification);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(70, (int)result.Value.CurrentQuantity);

        var uncommittedChanges = result.Value.GetUncommittedChanges();
        Assert.Single(uncommittedChanges);
        Assert.IsType<PartConsumedEvent>(uncommittedChanges[0]);
        var consumedEvent = (PartConsumedEvent)uncommittedChanges[0];
        Assert.Equal("ABC-123", consumedEvent.Sku.Value);
        Assert.Equal(30, (int)consumedEvent.Quantity);
        Assert.Equal("Used in production", consumedEvent.Justification);
    }

    [Fact]
    public void Consume_WithEmptyJustification_ReturnsFailure()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        var acquireQuantity = Quantity.Create(100).Value;
        part = part.Acquire(acquireQuantity, "Initial stock").Value;
        part.MarkChangesAsCommitted();

        var consumeQuantity = Quantity.Create(30).Value;
        var emptyJustification = "";

        // Act
        var result = part.Consume(consumeQuantity, emptyJustification);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("justification", result.Errors);
        Assert.Equal("Justification is required for consuming parts", result.Errors["justification"]);
    }

    [Fact]
    public void Consume_WithQuantityThatWouldGoNegative_ReturnsFailure()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        var acquireQuantity = Quantity.Create(50).Value;
        part = part.Acquire(acquireQuantity, "Initial stock").Value;
        part.MarkChangesAsCommitted();

        var consumeQuantity = Quantity.Create(100).Value; // More than available (50)
        var justification = "Over consumption";

        // Act
        var result = part.Consume(consumeQuantity, justification);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("quantity", result.Errors);
        Assert.Equal("Cannot consume more parts than are available", result.Errors["quantity"]);
    }

    [Fact]
    public void Recount_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        var newQuantity = Quantity.Create(75).Value;
        var justification = "Physical count";

        // Act
        var result = part.Recount(newQuantity, justification);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(75, (int)result.Value.CurrentQuantity);

        var uncommittedChanges = result.Value.GetUncommittedChanges();
        Assert.Single(uncommittedChanges);
        Assert.IsType<PartRecountedEvent>(uncommittedChanges[0]);
        var recountedEvent = (PartRecountedEvent)uncommittedChanges[0];
        Assert.Equal("ABC-123", recountedEvent.Sku.Value);
        Assert.Equal(75, (int)recountedEvent.Quantity);
        Assert.Equal("Physical count", recountedEvent.Justification);
    }

    [Fact]
    public void Recount_WithEmptyJustification_ReturnsFailure()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        var newQuantity = Quantity.Create(75).Value;
        var emptyJustification = "";

        // Act
        var result = part.Recount(newQuantity, emptyJustification);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("justification", result.Errors);
        Assert.Equal("Justification is required for recounting parts", result.Errors["justification"]);
    }

    [Fact]
    public void UpdateSource_WithValidSource_ReturnsSuccess()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var part = PartAggregate.Define(sku, name).Value;
        part.MarkChangesAsCommitted();

        var source = PartSource.Create("Supplier A", "https://supplier-a.com").Value;

        // Act
        var result = part.UpdateSource(source);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Source.HasValue);
        Assert.Equal("Supplier A", result.Value.Source.Value.Name);
        Assert.Equal("https://supplier-a.com", result.Value.Source.Value.Uri);

        var uncommittedChanges = result.Value.GetUncommittedChanges();
        Assert.Single(uncommittedChanges);
        Assert.IsType<PartSourceUpdatedEvent>(uncommittedChanges[0]);
        var sourceUpdatedEvent = (PartSourceUpdatedEvent)uncommittedChanges[0];
        Assert.Equal("ABC-123", sourceUpdatedEvent.Sku.Value);
        Assert.Equal("Supplier A", sourceUpdatedEvent.Source.Name);
        Assert.Equal("https://supplier-a.com", sourceUpdatedEvent.Source.Uri);
    }

    [Fact]
    public void Apply_PartDefinedEvent_SetsPropertiesCorrectly()
    {
        // Arrange
        var part = new PartAggregate();
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var @event = new PartDefinedEvent(sku, name);

        // Act
        part.ReplayEvents([@event]);

        // Assert
        Assert.Equal("ABC-123", part.AggregateId);
        Assert.Equal("ABC-123", part.Sku.Value);
        Assert.Equal("Widget A", part.Name.Value);
        Assert.Equal(0, (int)part.CurrentQuantity);
        Assert.True(part.Source.IsNone);
    }

    [Fact]
    public void Apply_PartAcquiredEvent_IncreasesQuantity()
    {
        // Arrange
        var part = new PartAggregate();
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var definedEvent = new PartDefinedEvent(sku, name);
        var acquiredEvent = new PartAcquiredEvent(sku, Quantity.Create(50).Value, "Initial stock");

        // Act
        part.ReplayEvents([definedEvent, acquiredEvent]);

        // Assert
        Assert.Equal(50, (int)part.CurrentQuantity);
    }

    [Fact]
    public void Apply_PartConsumedEvent_DecreasesQuantity()
    {
        // Arrange
        var part = new PartAggregate();
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var definedEvent = new PartDefinedEvent(sku, name);
        var acquiredEvent = new PartAcquiredEvent(sku, Quantity.Create(100).Value, "Initial stock");
        var consumedEvent = new PartConsumedEvent(sku, Quantity.Create(30).Value, "Used in production");

        // Act
        part.ReplayEvents([definedEvent, acquiredEvent, consumedEvent]);

        // Assert
        Assert.Equal(70, (int)part.CurrentQuantity);
    }

    [Fact]
    public void Apply_PartRecountedEvent_SetsQuantity()
    {
        // Arrange
        var part = new PartAggregate();
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var definedEvent = new PartDefinedEvent(sku, name);
        var recountedEvent = new PartRecountedEvent(sku, Quantity.Create(75).Value, "Physical count");

        // Act
        part.ReplayEvents([definedEvent, recountedEvent]);

        // Assert
        Assert.Equal(75, (int)part.CurrentQuantity);
    }

    [Fact]
    public void Apply_PartSourceUpdatedEvent_SetsSource()
    {
        // Arrange
        var part = new PartAggregate();
        var sku = PartSku.Create("ABC-123").Value;
        var name = PartName.Create("Widget A").Value;
        var definedEvent = new PartDefinedEvent(sku, name);
        var source = PartSource.Create("Supplier A", "https://supplier-a.com").Value;
        var sourceUpdatedEvent = new PartSourceUpdatedEvent(sku, source);

        // Act
        part.ReplayEvents([definedEvent, sourceUpdatedEvent]);

        // Assert
        Assert.True(part.Source.HasValue);
        Assert.Equal("Supplier A", part.Source.Value.Name);
        Assert.Equal("https://supplier-a.com", part.Source.Value.Uri);
    }
}