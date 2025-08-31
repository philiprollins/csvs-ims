using Application.Features.Product.ValueObjects;
using Library;

namespace Application.Features.Product;

public record ProductDefinedEvent(ProductSku Sku, ProductName Name) : Event(Sku.Value);

public record PartAddedToProductEvent(ProductSku ProductSku, ProductPart ProductPart) : Event(ProductSku.Value);

public class ProductAggregate : AggregateRoot
{
    public ProductSku Sku { get; private set; } = null!;
    public ProductName Name { get; private set; } = null!;
    public List<ProductPart> Parts { get; private set; } = [];

    public static Result<ProductAggregate> Define(ProductSku sku, ProductName name)
    {
        var product = new ProductAggregate();
        product.RaiseEvent(new ProductDefinedEvent(sku, name));
        return Result.Ok(product);
    }

    public Result<ProductAggregate> AddPart(ProductPart productPart)
    {
        if (Parts.Any(p => p.PartSku.Equals(productPart.PartSku)))
            return Result.Fail<ProductAggregate>("partSku", $"Part '{productPart.PartSku}' is already added to this product");

        RaiseEvent(new PartAddedToProductEvent(Sku, productPart));
        return Result.Ok(this);
    }

    public int GetPartCount() => Parts.Count;

    protected override void Apply(Event @event)
    {
        switch (@event)
        {
            case ProductDefinedEvent e:
                AggregateId = e.Sku.Value;
                Sku = e.Sku;
                Name = e.Name;
                Parts = [];
                break;

            case PartAddedToProductEvent e:
                Parts.Add(e.ProductPart);
                break;
        }
    }
}
