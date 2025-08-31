using Application.Features.Product.ValueObjects;
using Library;

namespace Application.Tests.Features.Part.ValueObjects;

public class ProductNameTests
{
    [Fact]
    public void Create_WithValidName_ReturnsSuccess()
    {
        // Arrange
        var validName = "Basic Desktop Computer";

        // Act
        var result = ProductName.Create(validName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Basic Desktop Computer", result.Value.Value);
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFailure()
    {
        // Arrange
        var emptyName = "";

        // Act
        var result = ProductName.Create(emptyName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("name", result.Errors);
        Assert.Equal("Product name cannot be empty", result.Errors["name"]);
    }

    [Fact]
    public void Create_WithWhitespaceName_ReturnsFailure()
    {
        // Arrange
        var whitespaceName = "   ";

        // Act
        var result = ProductName.Create(whitespaceName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("name", result.Errors);
        Assert.Equal("Product name cannot be empty", result.Errors["name"]);
    }

    [Fact]
    public void Create_WithNameTooLong_ReturnsFailure()
    {
        // Arrange
        var longName = new string('A', 201);

        // Act
        var result = ProductName.Create(longName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("name", result.Errors);
        Assert.Equal("Product name cannot exceed 200 characters", result.Errors["name"]);
    }

    [Fact]
    public void Create_WithValidNameAtMaxLength_ReturnsSuccess()
    {
        // Arrange
        var maxLengthName = new string('A', 200);

        // Act
        var result = ProductName.Create(maxLengthName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Value.Value.Length);
    }

    [Fact]
    public void Create_WithWhitespaceAroundName_TrimsWhitespace()
    {
        // Arrange
        var nameWithWhitespace = "  Basic Desktop Computer  ";

        // Act
        var result = ProductName.Create(nameWithWhitespace);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Basic Desktop Computer", result.Value.Value);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var name1 = ProductName.Create("Basic Desktop Computer").Value;
        var name2 = ProductName.Create("Basic Desktop Computer").Value;

        // Act & Assert
        Assert.Equal(name1, name2);
        Assert.True(name1.Equals(name2));
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var name1 = ProductName.Create("Basic Desktop Computer").Value;
        var name2 = ProductName.Create("Gaming PC").Value;

        // Act & Assert
        Assert.NotEqual(name1, name2);
        Assert.False(name1.Equals(name2));
    }

    [Fact]
    public void GetHashCode_WithSameValue_ReturnsSameHashCode()
    {
        // Arrange
        var name1 = ProductName.Create("Basic Desktop Computer").Value;
        var name2 = ProductName.Create("Basic Desktop Computer").Value;

        // Act & Assert
        Assert.Equal(name1.GetHashCode(), name2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var name = ProductName.Create("Basic Desktop Computer").Value;

        // Act
        var result = name.ToString();

        // Assert
        Assert.Equal("Basic Desktop Computer", result);
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsValue()
    {
        // Arrange
        var name = ProductName.Create("Basic Desktop Computer").Value;

        // Act
        string result = name;

        // Assert
        Assert.Equal("Basic Desktop Computer", result);
    }
}