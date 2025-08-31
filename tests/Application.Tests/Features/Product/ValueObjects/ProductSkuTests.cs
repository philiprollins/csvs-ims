using Application.Features.Product.ValueObjects;
using Library;

namespace Application.Tests.Features.Product.ValueObjects;

public class ProductSkuTests
{
    [Fact]
    public void Create_WithValidSku_ReturnsSuccess()
    {
        // Arrange
        var validSku = "PROD-001";

        // Act
        var result = ProductSku.Create(validSku);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("PROD-001", result.Value.Value);
    }

    [Fact]
    public void Create_WithEmptySku_ReturnsFailure()
    {
        // Arrange
        var emptySku = "";

        // Act
        var result = ProductSku.Create(emptySku);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("Product SKU cannot be empty", result.Errors["sku"]);
    }

    [Fact]
    public void Create_WithWhitespaceSku_ReturnsFailure()
    {
        // Arrange
        var whitespaceSku = "   ";

        // Act
        var result = ProductSku.Create(whitespaceSku);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("Product SKU cannot be empty", result.Errors["sku"]);
    }

    [Fact]
    public void Create_WithSkuTooLong_ReturnsFailure()
    {
        // Arrange
        var longSku = new string('A', 51);

        // Act
        var result = ProductSku.Create(longSku);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("Product SKU cannot exceed 50 characters", result.Errors["sku"]);
    }

    [Fact]
    public void Create_WithInvalidCharacters_ReturnsFailure()
    {
        // Arrange
        var invalidSku = "PROD_001";

        // Act
        var result = ProductSku.Create(invalidSku);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("Product SKU must contain only alphanumeric characters and hyphens", result.Errors["sku"]);
    }

    [Fact]
    public void Create_WithLowercaseSku_ConvertsToUppercase()
    {
        // Arrange
        var lowercaseSku = "prod-001";

        // Act
        var result = ProductSku.Create(lowercaseSku);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("PROD-001", result.Value.Value);
    }

    [Fact]
    public void Create_WithValidSkuAtMaxLength_ReturnsSuccess()
    {
        // Arrange
        var maxLengthSku = new string('A', 50);

        // Act
        var result = ProductSku.Create(maxLengthSku);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.Value.Length);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var sku1 = ProductSku.Create("PROD-001").Value;
        var sku2 = ProductSku.Create("PROD-001").Value;

        // Act & Assert
        Assert.Equal(sku1, sku2);
        Assert.True(sku1.Equals(sku2));
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var sku1 = ProductSku.Create("PROD-001").Value;
        var sku2 = ProductSku.Create("PROD-002").Value;

        // Act & Assert
        Assert.NotEqual(sku1, sku2);
        Assert.False(sku1.Equals(sku2));
    }

    [Fact]
    public void GetHashCode_WithSameValue_ReturnsSameHashCode()
    {
        // Arrange
        var sku1 = ProductSku.Create("PROD-001").Value;
        var sku2 = ProductSku.Create("PROD-001").Value;

        // Act & Assert
        Assert.Equal(sku1.GetHashCode(), sku2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var sku = ProductSku.Create("PROD-001").Value;

        // Act
        var result = sku.ToString();

        // Assert
        Assert.Equal("PROD-001", result);
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsValue()
    {
        // Arrange
        var sku = ProductSku.Create("PROD-001").Value;

        // Act
        string result = sku;

        // Assert
        Assert.Equal("PROD-001", result);
    }
}