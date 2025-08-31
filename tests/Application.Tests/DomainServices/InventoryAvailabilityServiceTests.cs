using Application.DomainServices;
using Application.Features.Part.Projections;
using Application.Features.Product.Projections;
using Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Application.Tests.DomainServices;

public class InventoryAvailabilityServiceTests : IDisposable
{
    private readonly PartsDbContext _dbContext;
    private readonly InventoryAvailabilityService _service;

    public InventoryAvailabilityServiceTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<PartsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new PartsDbContext(options);
        _service = new InventoryAvailabilityService(_dbContext);
    }

    #region CalculateBuildableQuantities Tests

    [Fact]
    public async Task CalculateBuildableQuantities_WithNullProductSkus_ReturnsFailure()
    {
        // Act
        var result = await _service.CalculateBuildableQuantities(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("productSkus", result.Errors);
        Assert.Equal("At least one product SKU must be provided", result.Errors["productSkus"]);
    }

    [Fact]
    public async Task CalculateBuildableQuantities_WithEmptyProductSkus_ReturnsFailure()
    {
        // Act
        var result = await _service.CalculateBuildableQuantities(new List<string>());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("productSkus", result.Errors);
        Assert.Equal("At least one product SKU must be provided", result.Errors["productSkus"]);
    }

    [Fact]
    public async Task CalculateBuildableQuantities_WithSingleProduct_ReturnsBuildabilityReport()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CalculateBuildableQuantities(new List<string> { "PROD-001" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Products);

        var product = result.Value.Products[0];
        Assert.Equal("PROD-001", product.Sku);
        Assert.Equal("Test Product 1", product.Name);
        Assert.Equal(2, product.BuildableQuantity); // Limited by PART-002 (10/5 = 2)

        Assert.Equal(2, product.Parts.Count);
        var part1 = product.Parts.First(p => p.PartSku == "PART-001");
        Assert.Equal("Part 1", part1.PartName);
        Assert.Equal(2, part1.RequiredQuantity);
        Assert.Equal(20, part1.AvailableQuantity);
        Assert.Equal(10, part1.BuildableFromThisPart);

        var part2 = product.Parts.First(p => p.PartSku == "PART-002");
        Assert.Equal("Part 2", part2.PartName);
        Assert.Equal(5, part2.RequiredQuantity);
        Assert.Equal(10, part2.AvailableQuantity);
        Assert.Equal(2, part2.BuildableFromThisPart);
    }

    [Fact]
    public async Task CalculateBuildableQuantities_WithMultipleProducts_ReturnsBuildabilityReport()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CalculateBuildableQuantities(new List<string> { "PROD-001", "PROD-002" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Products.Count);

        var product1 = result.Value.Products.First(p => p.Sku == "PROD-001");
        Assert.Equal(2, product1.BuildableQuantity);

        var product2 = result.Value.Products.First(p => p.Sku == "PROD-002");
        Assert.Equal("Test Product 2", product2.Name);
        Assert.Equal(0, product2.BuildableQuantity); // No parts required, so 0
    }

    [Fact]
    public async Task CalculateBuildableQuantities_WithNonExistentProduct_ReturnsUnknownProduct()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CalculateBuildableQuantities(new List<string> { "NONEXISTENT" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Products);

        var product = result.Value.Products[0];
        Assert.Equal("NONEXISTENT", product.Sku);
        Assert.Equal("Unknown Product", product.Name);
        Assert.Equal(0, product.BuildableQuantity);
        Assert.Empty(product.Parts);
    }

    [Fact]
    public async Task CalculateBuildableQuantities_WithDuplicateSkus_ReturnsDistinctResults()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CalculateBuildableQuantities(new List<string> { "PROD-001", "PROD-001" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Products);
        Assert.Equal("PROD-001", result.Value.Products[0].Sku);
    }

    [Fact]
    public async Task CalculateBuildableQuantities_WithProductHavingNoParts_ReturnsZeroBuildable()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CalculateBuildableQuantities(new List<string> { "PROD-002" });

        // Assert
        Assert.True(result.IsSuccess);
        var product = result.Value.Products[0];
        Assert.Equal("PROD-002", product.Sku);
        Assert.Equal(0, product.BuildableQuantity);
        Assert.Empty(product.Parts);
    }

    [Fact]
    public async Task CalculateBuildableQuantities_WithMissingPartInventory_ReturnsZeroBuildable()
    {
        // Arrange
        await SetupTestData();

        // Add product with missing part
        var product = new ProductDetailReadModel
        {
            Sku = "PROD-003",
            Name = "Product with Missing Part",
            PartCount = 1,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            PartTransactions = new List<ProductPartTransactionReadModel>
            {
                new() {
                    ProductSku = "PROD-003",
                    PartSku = "MISSING-PART",
                    Type = "PART_ADDED",
                    Quantity = 1,
                    Timestamp = DateTime.UtcNow
                }
            }
        };
        await _dbContext.ProductDetails.AddAsync(product);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.CalculateBuildableQuantities(new List<string> { "PROD-003" });

        // Assert
        Assert.True(result.IsSuccess);
        var resultProduct = result.Value.Products[0];
        Assert.Equal("PROD-003", resultProduct.Sku);
        Assert.Equal(0, resultProduct.BuildableQuantity);

        Assert.Single(resultProduct.Parts);
        var part = resultProduct.Parts[0];
        Assert.Equal("MISSING-PART", part.PartSku);
        Assert.Equal("Unknown Part", part.PartName);
        Assert.Equal(1, part.RequiredQuantity);
        Assert.Equal(0, part.AvailableQuantity);
        Assert.Equal(0, part.BuildableFromThisPart);
    }

    #endregion

    #region CalculateMaxBuildableForProduct Tests

    [Fact]
    public async Task CalculateMaxBuildableForProduct_WithNullSku_ReturnsFailure()
    {
        // Act
        var result = await _service.CalculateMaxBuildableForProduct(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("productSku", result.Errors);
        Assert.Equal("Product SKU cannot be empty", result.Errors["productSku"]);
    }

    [Fact]
    public async Task CalculateMaxBuildableForProduct_WithEmptySku_ReturnsFailure()
    {
        // Act
        var result = await _service.CalculateMaxBuildableForProduct("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("productSku", result.Errors);
        Assert.Equal("Product SKU cannot be empty", result.Errors["productSku"]);
    }

    [Fact]
    public async Task CalculateMaxBuildableForProduct_WithWhitespaceSku_ReturnsFailure()
    {
        // Act
        var result = await _service.CalculateMaxBuildableForProduct("   ");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("productSku", result.Errors);
        Assert.Equal("Product SKU cannot be empty", result.Errors["productSku"]);
    }

    [Fact]
    public async Task CalculateMaxBuildableForProduct_WithValidProduct_ReturnsBuildableQuantity()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CalculateMaxBuildableForProduct("PROD-001");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value); // Limited by PART-002 (10/5 = 2)
    }

    [Fact]
    public async Task CalculateMaxBuildableForProduct_WithNonExistentProduct_ReturnsZero()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CalculateMaxBuildableForProduct("NONEXISTENT");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task CalculateMaxBuildableForProduct_WithProductHavingNoParts_ReturnsZero()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CalculateMaxBuildableForProduct("PROD-002");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    #endregion

    #region CanBuildProduct Tests

    [Fact]
    public async Task CanBuildProduct_WithNullSku_ReturnsFailure()
    {
        // Act
        var result = await _service.CanBuildProduct(null!, 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("productSku", result.Errors);
        Assert.Equal("Product SKU cannot be empty", result.Errors["productSku"]);
    }

    [Fact]
    public async Task CanBuildProduct_WithEmptySku_ReturnsFailure()
    {
        // Act
        var result = await _service.CanBuildProduct("", 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("productSku", result.Errors);
        Assert.Equal("Product SKU cannot be empty", result.Errors["productSku"]);
    }

    [Fact]
    public async Task CanBuildProduct_WithZeroQuantity_ReturnsFailure()
    {
        // Act
        var result = await _service.CanBuildProduct("PROD-001", 0);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("requestedQuantity", result.Errors);
        Assert.Equal("Requested quantity must be greater than zero", result.Errors["requestedQuantity"]);
    }

    [Fact]
    public async Task CanBuildProduct_WithNegativeQuantity_ReturnsFailure()
    {
        // Act
        var result = await _service.CanBuildProduct("PROD-001", -1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("requestedQuantity", result.Errors);
        Assert.Equal("Requested quantity must be greater than zero", result.Errors["requestedQuantity"]);
    }

    [Fact]
    public async Task CanBuildProduct_WithValidRequestAndSufficientInventory_ReturnsTrue()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CanBuildProduct("PROD-001", 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task CanBuildProduct_WithValidRequestAndInsufficientInventory_ReturnsFalse()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CanBuildProduct("PROD-001", 5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task CanBuildProduct_WithNonExistentProduct_ReturnsFalse()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CanBuildProduct("NONEXISTENT", 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task CanBuildProduct_WithProductHavingNoParts_ReturnsFalse()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await _service.CanBuildProduct("PROD-002", 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    #endregion

    #region Helper Methods

    private async Task SetupTestData()
    {
        // Clear existing data
        _dbContext.PartSummary.RemoveRange(_dbContext.PartSummary);
        _dbContext.ProductDetails.RemoveRange(_dbContext.ProductDetails);
        await _dbContext.SaveChangesAsync();

        // Add parts
        await _dbContext.PartSummary.AddRangeAsync(
            new PartSummaryReadModel { Sku = "PART-001", Name = "Part 1", Quantity = 20 },
            new PartSummaryReadModel { Sku = "PART-002", Name = "Part 2", Quantity = 10 },
            new PartSummaryReadModel { Sku = "PART-003", Name = "Part 3", Quantity = 5 }
        );

        // Add products
        var product1 = new ProductDetailReadModel
        {
            Sku = "PROD-001",
            Name = "Test Product 1",
            PartCount = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastModified = DateTime.UtcNow,
            PartTransactions = new List<ProductPartTransactionReadModel>
            {
                new() {
                    ProductSku = "PROD-001",
                    PartSku = "PART-001",
                    Type = "PART_ADDED",
                    Quantity = 2,
                    Timestamp = DateTime.UtcNow.AddMinutes(-10)
                },
                new() {
                    ProductSku = "PROD-001",
                    PartSku = "PART-002",
                    Type = "PART_ADDED",
                    Quantity = 5,
                    Timestamp = DateTime.UtcNow.AddMinutes(-5)
                }
            }
        };

        var product2 = new ProductDetailReadModel
        {
            Sku = "PROD-002",
            Name = "Test Product 2",
            PartCount = 0,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastModified = DateTime.UtcNow,
            PartTransactions = new List<ProductPartTransactionReadModel>()
        };

        await _dbContext.ProductDetails.AddRangeAsync(product1, product2);
        await _dbContext.SaveChangesAsync();
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}