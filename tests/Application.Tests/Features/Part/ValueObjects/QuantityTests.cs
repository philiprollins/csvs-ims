using Application.Features.Part.ValueObjects;
using Library;

namespace Application.Tests.Features.Part.ValueObjects;

public class QuantityTests
{
    [Fact]
    public void Create_WithValidQuantity_ReturnsSuccess()
    {
        // Arrange
        var validQuantity = 100;

        // Act
        var result = Quantity.Create(validQuantity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.Value);
    }

    [Fact]
    public void Create_WithZeroQuantity_ReturnsSuccess()
    {
        // Arrange
        var zeroQuantity = 0;

        // Act
        var result = Quantity.Create(zeroQuantity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Value);
    }

    [Fact]
    public void Create_WithNegativeQuantity_ReturnsSuccess()
    {
        // Arrange
        var negativeQuantity = -50;

        // Act
        var result = Quantity.Create(negativeQuantity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(-50, result.Value.Value);
    }

    [Fact]
    public void Create_WithQuantityTooLarge_ReturnsFailure()
    {
        // Arrange
        var largeQuantity = 1000000;

        // Act
        var result = Quantity.Create(largeQuantity);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("quantity", result.Errors);
        Assert.Equal("Quantity exceeds maximum allowed (999,999)", result.Errors["quantity"]);
    }

    [Fact]
    public void Create_WithQuantityTooSmall_ReturnsFailure()
    {
        // Arrange
        var smallQuantity = -1000000;

        // Act
        var result = Quantity.Create(smallQuantity);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("quantity", result.Errors);
        Assert.Equal("Quantity is below minimum allowed (-999,999)", result.Errors["quantity"]);
    }

    [Fact]
    public void Create_WithMaxAllowedQuantity_ReturnsSuccess()
    {
        // Arrange
        var maxQuantity = 999999;

        // Act
        var result = Quantity.Create(maxQuantity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(999999, result.Value.Value);
    }

    [Fact]
    public void Create_WithMinAllowedQuantity_ReturnsSuccess()
    {
        // Arrange
        var minQuantity = -999999;

        // Act
        var result = Quantity.Create(minQuantity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(-999999, result.Value.Value);
    }

    [Fact]
    public void Add_WithValidQuantities_ReturnsSuccess()
    {
        // Arrange
        var quantity1 = Quantity.Create(100).Value;
        var quantity2 = Quantity.Create(50).Value;

        // Act
        var result = quantity1.Add(quantity2);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(150, result.Value.Value);
    }

    [Fact]
    public void Add_WithResultTooLarge_ReturnsFailure()
    {
        // Arrange
        var quantity1 = Quantity.Create(999999).Value;
        var quantity2 = Quantity.Create(1).Value;

        // Act
        var result = quantity1.Add(quantity2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("quantity", result.Errors);
        Assert.Equal("Quantity exceeds maximum allowed (999,999)", result.Errors["quantity"]);
    }

    [Fact]
    public void Subtract_WithValidQuantities_ReturnsSuccess()
    {
        // Arrange
        var quantity1 = Quantity.Create(100).Value;
        var quantity2 = Quantity.Create(50).Value;

        // Act
        var result = quantity1.Subtract(quantity2);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.Value);
    }

    [Fact]
    public void Subtract_WithResultTooSmall_ReturnsFailure()
    {
        // Arrange
        var quantity1 = Quantity.Create(-999999).Value;
        var quantity2 = Quantity.Create(1).Value;

        // Act
        var result = quantity1.Subtract(quantity2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("quantity", result.Errors);
        Assert.Equal("Quantity is below minimum allowed (-999,999)", result.Errors["quantity"]);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var quantity1 = Quantity.Create(100).Value;
        var quantity2 = Quantity.Create(100).Value;

        // Act & Assert
        Assert.Equal(quantity1, quantity2);
        Assert.True(quantity1.Equals(quantity2));
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var quantity1 = Quantity.Create(100).Value;
        var quantity2 = Quantity.Create(200).Value;

        // Act & Assert
        Assert.NotEqual(quantity1, quantity2);
        Assert.False(quantity1.Equals(quantity2));
    }

    [Fact]
    public void GetHashCode_WithSameValue_ReturnsSameHashCode()
    {
        // Arrange
        var quantity1 = Quantity.Create(100).Value;
        var quantity2 = Quantity.Create(100).Value;

        // Act & Assert
        Assert.Equal(quantity1.GetHashCode(), quantity2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValueAsString()
    {
        // Arrange
        var quantity = Quantity.Create(100).Value;

        // Act
        var result = quantity.ToString();

        // Assert
        Assert.Equal("100", result);
    }

    [Fact]
    public void ImplicitOperatorInt_ReturnsValue()
    {
        // Arrange
        var quantity = Quantity.Create(100).Value;

        // Act
        int result = quantity;

        // Assert
        Assert.Equal(100, result);
    }
}