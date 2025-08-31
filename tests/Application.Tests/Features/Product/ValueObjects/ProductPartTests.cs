using Application.Features.Part.ValueObjects;
using Application.Features.Product.ValueObjects;
using Library;

namespace Application.Tests.Features.Product.ValueObjects;

public class ProductPartTests
{
    [Fact]
    public void Create_WithValidPartSkuAndQuantity_ReturnsSuccess()
    {
        // Arrange
        var partSku = "PART-001";
        var quantity = 5;

        // Act
        var result = ProductPart.Create(partSku, quantity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("PART-001", result.Value.PartSku.Value);
        Assert.Equal(5, (int)result.Value.Quantity);
    }

    [Fact]
    public void Create_WithInvalidPartSku_ReturnsFailure()
    {
        // Arrange
        var invalidPartSku = "";
        var quantity = 5;

        // Act
        var result = ProductPart.Create(invalidPartSku, quantity);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("Part SKU cannot be empty", result.Errors["sku"]);
    }

    [Fact]
    public void Create_WithInvalidQuantity_ReturnsFailure()
    {
        // Arrange
        var partSku = "PART-001";
        var invalidQuantity = -1;

        // Act
        var result = ProductPart.Create(partSku, invalidQuantity);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("quantity", result.Errors);
        Assert.Equal("Part quantity must be greater than 0", result.Errors["quantity"]);
    }

    [Fact]
    public void Create_WithZeroQuantity_ReturnsFailure()
    {
        // Arrange
        var partSku = "PART-001";
        var zeroQuantity = 0;

        // Act
        var result = ProductPart.Create(partSku, zeroQuantity);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("quantity", result.Errors);
        Assert.Equal("Part quantity must be greater than 0", result.Errors["quantity"]);
    }

    [Fact]
    public void Create_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var invalidPartSku = "";
        var invalidQuantity = -1000000; // This will fail Quantity validation

        // Act
        var result = ProductPart.Create(invalidPartSku, invalidQuantity);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Contains("quantity", result.Errors);
        Assert.Equal("Part SKU cannot be empty", result.Errors["sku"]);
        Assert.Equal("Quantity is below minimum allowed (-999,999)", result.Errors["quantity"]);
    }

    [Fact]
    public void Equals_WithSamePartSkuAndQuantity_ReturnsTrue()
    {
        // Arrange
        var part1 = ProductPart.Create("PART-001", 5).Value;
        var part2 = ProductPart.Create("PART-001", 5).Value;

        // Act & Assert
        Assert.Equal(part1, part2);
        Assert.True(part1.Equals(part2));
    }

    [Fact]
    public void Equals_WithDifferentPartSku_ReturnsFalse()
    {
        // Arrange
        var part1 = ProductPart.Create("PART-001", 5).Value;
        var part2 = ProductPart.Create("PART-002", 5).Value;

        // Act & Assert
        Assert.NotEqual(part1, part2);
        Assert.False(part1.Equals(part2));
    }

    [Fact]
    public void Equals_WithDifferentQuantity_ReturnsFalse()
    {
        // Arrange
        var part1 = ProductPart.Create("PART-001", 5).Value;
        var part2 = ProductPart.Create("PART-001", 10).Value;

        // Act & Assert
        Assert.NotEqual(part1, part2);
        Assert.False(part1.Equals(part2));
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var part1 = ProductPart.Create("PART-001", 5).Value;
        var part2 = ProductPart.Create("PART-001", 5).Value;

        // Act & Assert
        Assert.Equal(part1.GetHashCode(), part2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ReturnsDifferentHashCode()
    {
        // Arrange
        var part1 = ProductPart.Create("PART-001", 5).Value;
        var part2 = ProductPart.Create("PART-002", 5).Value;

        // Act & Assert
        Assert.NotEqual(part1.GetHashCode(), part2.GetHashCode());
    }
}