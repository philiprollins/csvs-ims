using Application.Features.Part.ValueObjects;
using Library;
using System.Text.Json;
using System.Text.Json.Serialization;

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

public class ProductPartJsonConverter : JsonConverter<ProductPart>
{
    public override ProductPart Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var partSku = PartSku.Create(root.GetProperty("PartSku").GetString()!).Value;
        var quantity = Quantity.Create(root.GetProperty("Quantity").GetInt32()).Value;

        return ProductPart.FromTrustedSource(partSku, quantity);
    }

    public override void Write(Utf8JsonWriter writer, ProductPart value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("PartSku", value.PartSku.Value);
        writer.WriteNumber("Quantity", value.Quantity.Value);
        writer.WriteEndObject();
    }
}