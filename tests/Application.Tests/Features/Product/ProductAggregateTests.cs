using Application.Features.Product;
using Application.Features.Product.ValueObjects;
using Application.Features.Part.ValueObjects;
using Library;

namespace Application.Tests.Features.Product;

public class ProductAggregateTests
{
    [Fact]
    public void Define_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;

        // Act
        var result = ProductAggregate.Define(sku, name);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("PROD-001", result.Value.Sku.Value);
        Assert.Equal("Basic Desktop Computer", result.Value.Name.Value);
        Assert.Empty(result.Value.Parts);
        Assert.Equal(0, result.Value.GetPartCount());

        var uncommittedChanges = result.Value.GetUncommittedChanges();
        Assert.Single(uncommittedChanges);
        Assert.IsType<ProductDefinedEvent>(uncommittedChanges[0]);
        var definedEvent = (ProductDefinedEvent)uncommittedChanges[0];
        Assert.Equal("PROD-001", definedEvent.Sku.Value);
        Assert.Equal("Basic Desktop Computer", definedEvent.Name.Value);
    }

    [Fact]
    public void AddPart_WithValidPart_ReturnsSuccess()
    {
        // Arrange
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;
        var product = ProductAggregate.Define(sku, name).Value;
        product.MarkChangesAsCommitted();

        var productPart = ProductPart.Create("PART-001", 2).Value;

        // Act
        var result = product.AddPart(productPart);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Parts);
        Assert.Equal("PART-001", result.Value.Parts[0].PartSku.Value);
        Assert.Equal(2, (int)result.Value.Parts[0].Quantity);
        Assert.Equal(1, result.Value.GetPartCount());

        var uncommittedChanges = result.Value.GetUncommittedChanges();
        Assert.Single(uncommittedChanges);
        Assert.IsType<PartAddedToProductEvent>(uncommittedChanges[0]);
        var partAddedEvent = (PartAddedToProductEvent)uncommittedChanges[0];
        Assert.Equal("PROD-001", partAddedEvent.ProductSku.Value);
        Assert.Equal("PART-001", partAddedEvent.ProductPart.PartSku.Value);
        Assert.Equal(2, (int)partAddedEvent.ProductPart.Quantity);
    }

    [Fact]
    public void AddPart_WithDuplicatePartSku_ReturnsFailure()
    {
        // Arrange
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;
        var product = ProductAggregate.Define(sku, name).Value;
        product.MarkChangesAsCommitted();

        var productPart1 = ProductPart.Create("PART-001", 2).Value;
        var productPart2 = ProductPart.Create("PART-001", 3).Value; // Same part SKU

        // Act
        product = product.AddPart(productPart1).Value;
        product.MarkChangesAsCommitted();
        var result = product.AddPart(productPart2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("partSku", result.Errors);
        Assert.Equal("Part 'PART-001' is already added to this product", result.Errors["partSku"]);
    }

    [Fact]
    public void AddPart_WithMultipleDifferentParts_ReturnsSuccess()
    {
        // Arrange
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;
        var product = ProductAggregate.Define(sku, name).Value;
        product.MarkChangesAsCommitted();

        var productPart1 = ProductPart.Create("PART-001", 1).Value;
        var productPart2 = ProductPart.Create("PART-002", 2).Value;
        var productPart3 = ProductPart.Create("PART-003", 1).Value;

        // Act
        product = product.AddPart(productPart1).Value;
        product.MarkChangesAsCommitted();
        product = product.AddPart(productPart2).Value;
        product.MarkChangesAsCommitted();
        var result = product.AddPart(productPart3);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Parts.Count);
        Assert.Equal(3, result.Value.GetPartCount());

        // Verify all parts are present
        Assert.Contains(result.Value.Parts, p => p.PartSku.Value == "PART-001" && p.Quantity.Value == 1);
        Assert.Contains(result.Value.Parts, p => p.PartSku.Value == "PART-002" && p.Quantity.Value == 2);
        Assert.Contains(result.Value.Parts, p => p.PartSku.Value == "PART-003" && p.Quantity.Value == 1);
    }



    [Fact]
    public void GetPartCount_WithNoParts_ReturnsZero()
    {
        // Arrange
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;
        var product = ProductAggregate.Define(sku, name).Value;

        // Act
        var partCount = product.GetPartCount();

        // Assert
        Assert.Equal(0, partCount);
    }

    [Fact]
    public void Apply_ProductDefinedEvent_SetsPropertiesCorrectly()
    {
        // Arrange
        var product = new ProductAggregate();
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;
        var @event = new ProductDefinedEvent(sku, name);

        // Act
        product.ReplayEvents([@event]);

        // Assert
        Assert.Equal("PROD-001", product.AggregateId);
        Assert.Equal("PROD-001", product.Sku.Value);
        Assert.Equal("Basic Desktop Computer", product.Name.Value);
        Assert.Empty(product.Parts);
        Assert.Equal(0, product.GetPartCount());
    }

    [Fact]
    public void Apply_PartAddedToProductEvent_AddsPart()
    {
        // Arrange
        var product = new ProductAggregate();
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;
        var definedEvent = new ProductDefinedEvent(sku, name);
        var productPart = ProductPart.Create("PART-001", 2).Value;
        var partAddedEvent = new PartAddedToProductEvent(sku, productPart);

        // Act
        product.ReplayEvents([definedEvent, partAddedEvent]);

        // Assert
        Assert.Single(product.Parts);
        Assert.Equal("PART-001", product.Parts[0].PartSku.Value);
        Assert.Equal(2, (int)product.Parts[0].Quantity);
        Assert.Equal(1, product.GetPartCount());
    }

    [Fact]
    public void Apply_MultiplePartAddedToProductEvents_AddsAllParts()
    {
        // Arrange
        var product = new ProductAggregate();
        var sku = ProductSku.Create("PROD-001").Value;
        var name = ProductName.Create("Basic Desktop Computer").Value;
        var definedEvent = new ProductDefinedEvent(sku, name);
        
        var productPart1 = ProductPart.Create("PART-001", 1).Value;
        var productPart2 = ProductPart.Create("PART-002", 2).Value;
        var productPart3 = ProductPart.Create("PART-003", 1).Value;
        
        var partAddedEvent1 = new PartAddedToProductEvent(sku, productPart1);
        var partAddedEvent2 = new PartAddedToProductEvent(sku, productPart2);
        var partAddedEvent3 = new PartAddedToProductEvent(sku, productPart3);

        // Act
        product.ReplayEvents([definedEvent, partAddedEvent1, partAddedEvent2, partAddedEvent3]);

        // Assert
        Assert.Equal(3, product.Parts.Count);
        Assert.Equal(3, product.GetPartCount());
        
        // Verify all parts are present
        Assert.Contains(product.Parts, p => p.PartSku.Value == "PART-001" && p.Quantity.Value == 1);
        Assert.Contains(product.Parts, p => p.PartSku.Value == "PART-002" && p.Quantity.Value == 2);
        Assert.Contains(product.Parts, p => p.PartSku.Value == "PART-003" && p.Quantity.Value == 1);
    }
}