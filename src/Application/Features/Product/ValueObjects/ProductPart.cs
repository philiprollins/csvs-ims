using Application.Features.Part.ValueObjects;
using Library;

namespace Application.Features.Product.ValueObjects;

public class ProductPart
{
    public PartSku PartSku { get; private set; }
    public Quantity Quantity { get; private set; }

    private ProductPart(PartSku partSku, Quantity quantity)
    {
        PartSku = partSku;
        Quantity = quantity;
    }

    public static Result<ProductPart> Create(string partSku, int quantity)
    {
        var partSkuResult = PartSku.Create(partSku);
        var quantityResult = Quantity.Create(quantity);

        var combined = Result.Combine(partSkuResult, quantityResult);

        if (combined.IsFailure)
            return Result.Fail<ProductPart>(combined.Errors);

        if (quantity <= 0)
            return Result.Fail<ProductPart>("quantity", "Part quantity must be greater than 0");

        return Result.Ok(new ProductPart(partSkuResult.Value, quantityResult.Value));
    }

    public override bool Equals(object? obj) =>
        obj is ProductPart other && PartSku.Equals(other.PartSku) && Quantity.Equals(other.Quantity);

    public override int GetHashCode() => HashCode.Combine(PartSku, Quantity);

    internal static ProductPart FromTrustedSource(PartSku partSku, Quantity quantity) =>
        new(partSku, quantity);
}