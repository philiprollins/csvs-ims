using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Product.Projections;

public class ProductSummaryReadModel
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PartCount { get; set; } = 0;
}

public class ProductDetailReadModel
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PartCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }

    public List<ProductPartTransactionReadModel> PartTransactions { get; set; } = new();
}

public class ProductPartTransactionReadModel
{
    public int Id { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string PartSku { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Timestamp { get; set; }

    public ProductDetailReadModel Product { get; set; } = null!;
}

public class ProductSummaryProjection(PartsDbContext db) :
    IEventHandler<ProductDefinedEvent>,
    IEventHandler<PartAddedToProductEvent>
{
    public async Task HandleAsync(ProductDefinedEvent @event, CancellationToken cancellationToken = default)
    {
        var product = await db.ProductSummary.SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        if (product == null)
        {
            product = new ProductSummaryReadModel { Sku = @event.Sku.Value };
            db.ProductSummary.Add(product);
        }
        product.Name = @event.Name.Value;
        product.PartCount = 0;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(PartAddedToProductEvent @event, CancellationToken cancellationToken = default)
    {
        var product = await db.ProductSummary.SingleOrDefaultAsync(p => p.Sku == @event.ProductSku.Value, cancellationToken);
        if (product != null)
        {
            product.PartCount += 1;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

public class ProductDetailProjection(PartsDbContext db) :
    IEventHandler<ProductDefinedEvent>,
    IEventHandler<PartAddedToProductEvent>
{
    public async Task HandleAsync(ProductDefinedEvent @event, CancellationToken cancellationToken = default)
    {
        var existingProduct = await db.ProductDetails.SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        if (existingProduct == null)
        {
            var product = new ProductDetailReadModel
            {
                Sku = @event.Sku.Value,
                Name = @event.Name.Value,
                PartCount = 0,
                CreatedAt = @event.Timestamp,
                LastModified = @event.Timestamp
            };

            db.ProductDetails.Add(product);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(PartAddedToProductEvent @event, CancellationToken cancellationToken = default)
    {
        var product = await db.ProductDetails
            .Include(p => p.PartTransactions)
            .SingleOrDefaultAsync(p => p.Sku == @event.ProductSku.Value, cancellationToken);

        if (product != null)
        {
            product.PartCount += 1;
            product.LastModified = @event.Timestamp;

            var transaction = new ProductPartTransactionReadModel
            {
                ProductSku = @event.ProductSku.Value,
                PartSku = @event.ProductPart.PartSku.Value,
                Type = "PART_ADDED",
                Quantity = @event.ProductPart.Quantity.Value,
                Timestamp = @event.Timestamp,
                Product = product
            };

            product.PartTransactions.Add(transaction);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}