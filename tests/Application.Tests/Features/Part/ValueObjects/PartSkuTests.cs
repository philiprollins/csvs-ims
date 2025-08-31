using Application.Features.Part.ValueObjects;
using Library;

namespace Application.Tests.Features.Part.ValueObjects;

public class PartSkuTests
{
    [Fact]
    public void Create_WithValidSku_ReturnsSuccess()
    {
        // Arrange
        var validSku = "ABC-123";

        // Act
        var result = PartSku.Create(validSku);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ABC-123", result.Value.Value);
    }

    [Fact]
    public void Create_WithEmptySku_ReturnsFailure()
    {
        // Arrange
        var emptySku = "";

        // Act
        var result = PartSku.Create(emptySku);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("Part SKU cannot be empty", result.Errors["sku"]);
    }

    [Fact]
    public void Create_WithWhitespaceSku_ReturnsFailure()
    {
        // Arrange
        var whitespaceSku = "   ";

        // Act
        var result = PartSku.Create(whitespaceSku);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("Part SKU cannot be empty", result.Errors["sku"]);
    }

    [Fact]
    public void Create_WithSkuTooLong_ReturnsFailure()
    {
        // Arrange
        var longSku = new string('A', 51);

        // Act
        var result = PartSku.Create(longSku);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("Part SKU cannot exceed 50 characters", result.Errors["sku"]);
    }

    [Fact]
    public void Create_WithInvalidCharacters_ReturnsFailure()
    {
        // Arrange
        var invalidSku = "ABC_123";

        // Act
        var result = PartSku.Create(invalidSku);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sku", result.Errors);
        Assert.Equal("Part SKU must contain only alphanumeric characters and hyphens", result.Errors["sku"]);
    }

    [Fact]
    public void Create_WithLowercaseSku_ConvertsToUppercase()
    {
        // Arrange
        var lowercaseSku = "abc-123";

        // Act
        var result = PartSku.Create(lowercaseSku);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ABC-123", result.Value.Value);
    }

    [Fact]
    public void Create_WithValidSkuAtMaxLength_ReturnsSuccess()
    {
        // Arrange
        var maxLengthSku = new string('A', 50);

        // Act
        var result = PartSku.Create(maxLengthSku);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.Value.Length);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var sku1 = PartSku.Create("ABC-123").Value;
        var sku2 = PartSku.Create("ABC-123").Value;

        // Act & Assert
        Assert.Equal(sku1, sku2);
        Assert.True(sku1.Equals(sku2));
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var sku1 = PartSku.Create("ABC-123").Value;
        var sku2 = PartSku.Create("XYZ-456").Value;

        // Act & Assert
        Assert.NotEqual(sku1, sku2);
        Assert.False(sku1.Equals(sku2));
    }

    [Fact]
    public void GetHashCode_WithSameValue_ReturnsSameHashCode()
    {
        // Arrange
        var sku1 = PartSku.Create("ABC-123").Value;
        var sku2 = PartSku.Create("ABC-123").Value;

        // Act & Assert
        Assert.Equal(sku1.GetHashCode(), sku2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;

        // Act
        var result = sku.ToString();

        // Assert
        Assert.Equal("ABC-123", result);
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsValue()
    {
        // Arrange
        var sku = PartSku.Create("ABC-123").Value;

        // Act
        string result = sku;

        // Assert
        Assert.Equal("ABC-123", result);
    }
}