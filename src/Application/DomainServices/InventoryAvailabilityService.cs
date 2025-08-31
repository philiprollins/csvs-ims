using Library;
using Microsoft.EntityFrameworkCore;

namespace Application.DomainServices;
// Domain service response objects
public class BuildabilityReport
{
    private readonly List<ProductBuildability> _products = new();
    public IReadOnlyList<ProductBuildability> Products => _products.AsReadOnly();

    public void AddProduct(ProductBuildability product) => _products.Add(product);
}

public class ProductBuildability
{
    public string Sku { get; }
    public string Name { get; }
    public int BuildableQuantity { get; }
    public IReadOnlyList<BuildablePartInfo> Parts { get; }

    public ProductBuildability(string sku, string name, int buildableQuantity, List<BuildablePartInfo> parts)
    {
        Sku = sku;
        Name = name;
        BuildableQuantity = buildableQuantity;
        Parts = parts.AsReadOnly();
    }
}

public class BuildablePartInfo
{
    public string PartSku { get; }
    public string PartName { get; }
    public int RequiredQuantity { get; }
    public int AvailableQuantity { get; }
    public int BuildableFromThisPart { get; }

    public BuildablePartInfo(string partSku, string partName, int requiredQuantity, int availableQuantity, int buildableFromThisPart)
    {
        PartSku = partSku;
        PartName = partName;
        RequiredQuantity = requiredQuantity;
        AvailableQuantity = availableQuantity;
        BuildableFromThisPart = buildableFromThisPart;
    }
}

// Domain Service Implementation
public class InventoryAvailabilityService
{
    private readonly PartsDbContext _dbContext;

    public InventoryAvailabilityService(PartsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BuildabilityReport>> CalculateBuildableQuantities(
        List<string> productSkus, 
        CancellationToken cancellationToken = default)
    {
        if (productSkus == null || !productSkus.Any())
            return Result.Fail<BuildabilityReport>("productSkus", "At least one product SKU must be provided");

        var report = new BuildabilityReport();

        foreach (var productSku in productSkus.Distinct())
        {
            var buildability = await CalculateProductBuildability(productSku, cancellationToken);
            report.AddProduct(buildability);
        }

        return Result.Ok(report);
    }

    private async Task<ProductBuildability> CalculateProductBuildability(
        string productSku, 
        CancellationToken cancellationToken)
    {
        // Get product details with part transactions (which contain the product's bill of materials)
        var product = await _dbContext.ProductDetails
            .Include(p => p.PartTransactions)
            .FirstOrDefaultAsync(p => p.Sku == productSku, cancellationToken);

        if (product == null)
        {
            return new ProductBuildability(productSku, "Unknown Product", 0, new List<BuildablePartInfo>());
        }

        // Extract unique parts and their required quantities from transactions
        var partRequirements = product.PartTransactions
            .Where(t => t.Type == "PART_ADDED")
            .GroupBy(t => t.PartSku)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Quantity));

        if (!partRequirements.Any())
        {
            return new ProductBuildability(product.Sku, product.Name, 0, new List<BuildablePartInfo>());
        }

        var partSkus = partRequirements.Keys.ToList();
        
        // Get current inventory levels for all required parts
        var partInventories = await _dbContext.PartSummary
            .Where(p => partSkus.Contains(p.Sku))
            .ToDictionaryAsync(p => p.Sku, p => new { p.Name, p.Quantity }, cancellationToken);

        var partInfos = new List<BuildablePartInfo>();
        var maxBuildable = int.MaxValue;

        foreach (var (partSku, requiredQuantity) in partRequirements)
        {
            if (partInventories.TryGetValue(partSku, out var inventory))
            {
                var buildableFromThisPart = requiredQuantity > 0 ? inventory.Quantity / requiredQuantity : 0;
                maxBuildable = Math.Min(maxBuildable, buildableFromThisPart);

                partInfos.Add(new BuildablePartInfo(
                    partSku,
                    inventory.Name,
                    requiredQuantity,
                    inventory.Quantity,
                    buildableFromThisPart));
            }
            else
            {
                // Part doesn't exist in inventory
                maxBuildable = 0;
                partInfos.Add(new BuildablePartInfo(
                    partSku,
                    "Unknown Part",
                    requiredQuantity,
                    0,
                    0));
            }
        }

        // Handle case where product has no part requirements
        maxBuildable = maxBuildable == int.MaxValue ? 0 : maxBuildable;

        return new ProductBuildability(product.Sku, product.Name, maxBuildable, partInfos);
    }

    public async Task<Result<int>> CalculateMaxBuildableForProduct(
        string productSku, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productSku))
            return Result.Fail<int>("productSku", "Product SKU cannot be empty");

        var buildability = await CalculateProductBuildability(productSku, cancellationToken);
        return Result.Ok(buildability.BuildableQuantity);
    }

    public async Task<Result<bool>> CanBuildProduct(
        string productSku, 
        int requestedQuantity, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productSku))
            return Result.Fail<bool>("productSku", "Product SKU cannot be empty");
        
        if (requestedQuantity <= 0)
            return Result.Fail<bool>("requestedQuantity", "Requested quantity must be greater than zero");

        var maxBuildableResult = await CalculateMaxBuildableForProduct(productSku, cancellationToken);
        if (!maxBuildableResult.IsSuccess)
            return Result.Fail<bool>(maxBuildableResult.Errors);

        return Result.Ok(maxBuildableResult.Value >= requestedQuantity);
    }
}