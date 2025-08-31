using Application.Features.Part.ValueObjects;
using Library;

namespace Application.Tests.Features.Part.ValueObjects;

public class PartSourceTests
{
    [Fact]
    public void Create_WithValidSource_ReturnsSuccess()
    {
        // Arrange
        var validName = "Supplier A";
        var validUri = "https://supplier-a.com";

        // Act
        var result = PartSource.Create(validName, validUri);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Supplier A", result.Value.Name);
        Assert.Equal("https://supplier-a.com", result.Value.Uri);
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFailure()
    {
        // Arrange
        var emptyName = "";
        var validUri = "https://supplier-a.com";

        // Act
        var result = PartSource.Create(emptyName, validUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sourceName", result.Errors);
        Assert.Equal("Source name cannot be empty", result.Errors["sourceName"]);
    }

    [Fact]
    public void Create_WithWhitespaceName_ReturnsFailure()
    {
        // Arrange
        var whitespaceName = "   ";
        var validUri = "https://supplier-a.com";

        // Act
        var result = PartSource.Create(whitespaceName, validUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sourceName", result.Errors);
        Assert.Equal("Source name cannot be empty", result.Errors["sourceName"]);
    }

    [Fact]
    public void Create_WithNameTooLong_ReturnsFailure()
    {
        // Arrange
        var longName = new string('A', 101);
        var validUri = "https://supplier-a.com";

        // Act
        var result = PartSource.Create(longName, validUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sourceName", result.Errors);
        Assert.Equal("Source name cannot exceed 100 characters", result.Errors["sourceName"]);
    }

    [Fact]
    public void Create_WithEmptyUri_ReturnsFailure()
    {
        // Arrange
        var validName = "Supplier A";
        var emptyUri = "";

        // Act
        var result = PartSource.Create(validName, emptyUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sourceUri", result.Errors);
        Assert.Equal("Source URI cannot be empty", result.Errors["sourceUri"]);
    }

    [Fact]
    public void Create_WithWhitespaceUri_ReturnsFailure()
    {
        // Arrange
        var validName = "Supplier A";
        var whitespaceUri = "   ";

        // Act
        var result = PartSource.Create(validName, whitespaceUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sourceUri", result.Errors);
        Assert.Equal("Source URI cannot be empty", result.Errors["sourceUri"]);
    }

    [Fact]
    public void Create_WithUriTooLong_ReturnsFailure()
    {
        // Arrange
        var validName = "Supplier A";
        var longUri = "https://" + new string('a', 190) + ".com";

        // Act
        var result = PartSource.Create(validName, longUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sourceUri", result.Errors);
        Assert.Equal("Source URI cannot exceed 200 characters", result.Errors["sourceUri"]);
    }

    [Fact]
    public void Create_WithInvalidUri_ReturnsFailure()
    {
        // Arrange
        var validName = "Supplier A";
        var invalidUri = "not-a-valid-uri";

        // Act
        var result = PartSource.Create(validName, invalidUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sourceUri", result.Errors);
        Assert.Equal("Source URI is not a valid absolute URI", result.Errors["sourceUri"]);
    }

    [Fact]
    public void Create_WithRelativeUri_ReturnsFailure()
    {
        // Arrange
        var validName = "Supplier A";
        var relativeUri = "/path/to/resource";

        // Act
        var result = PartSource.Create(validName, relativeUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sourceUri", result.Errors);
        Assert.Equal("Source URI is not a valid absolute URI", result.Errors["sourceUri"]);
    }

    [Fact]
    public void Create_WithValidSourceAtMaxLengths_ReturnsSuccess()
    {
        // Arrange
        var maxLengthName = new string('A', 100);
        var maxLengthUri = "https://" + new string('a', 188) + ".com"; // 200 chars total: https:// (8) + 188 a's + .com (4) = 200

        // Act
        var result = PartSource.Create(maxLengthName, maxLengthUri);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.Name.Length);
        Assert.Equal(200, result.Value.Uri.Length);
    }

    [Fact]
    public void Create_WithNameAndUriWithWhitespace_TrimsWhitespace()
    {
        // Arrange
        var nameWithWhitespace = "  Supplier A  ";
        var uriWithWhitespace = "  https://supplier-a.com  ";

        // Act
        var result = PartSource.Create(nameWithWhitespace, uriWithWhitespace);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Supplier A", result.Value.Name);
        Assert.Equal("https://supplier-a.com", result.Value.Uri);
    }

    [Fact]
    public void Create_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var emptyName = "";
        var invalidUri = "not-a-valid-uri";

        // Act
        var result = PartSource.Create(emptyName, invalidUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("sourceName", result.Errors);
        Assert.Contains("sourceUri", result.Errors);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var source1 = PartSource.Create("Supplier A", "https://supplier-a.com").Value;
        var source2 = PartSource.Create("Supplier A", "https://supplier-a.com").Value;

        // Act & Assert
        Assert.Equal(source1, source2);
        Assert.True(source1.Equals(source2));
    }

    [Fact]
    public void Equals_WithDifferentName_ReturnsFalse()
    {
        // Arrange
        var source1 = PartSource.Create("Supplier A", "https://supplier-a.com").Value;
        var source2 = PartSource.Create("Supplier B", "https://supplier-a.com").Value;

        // Act & Assert
        Assert.NotEqual(source1, source2);
        Assert.False(source1.Equals(source2));
    }

    [Fact]
    public void Equals_WithDifferentUri_ReturnsFalse()
    {
        // Arrange
        var source1 = PartSource.Create("Supplier A", "https://supplier-a.com").Value;
        var source2 = PartSource.Create("Supplier A", "https://supplier-b.com").Value;

        // Act & Assert
        Assert.NotEqual(source1, source2);
        Assert.False(source1.Equals(source2));
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var source1 = PartSource.Create("Supplier A", "https://supplier-a.com").Value;
        var source2 = PartSource.Create("Supplier A", "https://supplier-a.com").Value;

        // Act & Assert
        Assert.Equal(source1.GetHashCode(), source2.GetHashCode());
    }
}