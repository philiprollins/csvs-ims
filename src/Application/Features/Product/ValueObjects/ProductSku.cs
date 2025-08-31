using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Library;

namespace Application.Features.Product.ValueObjects;

public partial class ProductSku
{
    public string Value { get; private set; }

    private ProductSku(string value) => Value = value;

    public static Result<ProductSku> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail<ProductSku>("sku", "Product SKU cannot be empty");
        if (value.Length > 50)
            return Result.Fail<ProductSku>("sku", "Product SKU cannot exceed 50 characters");
        if (!IsValidSkuFormat(value))
            return Result.Fail<ProductSku>("sku", "Product SKU must contain only alphanumeric characters and hyphens");

        return Result.Ok(new ProductSku(value.ToUpperInvariant()));
    }

    private static bool IsValidSkuFormat(string value) =>
        MyRegex().IsMatch(value);

    public override bool Equals(object? obj) => obj is ProductSku other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator string(ProductSku sku) => sku.Value;
    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Za-z0-9\-]+$")]
    private static partial Regex MyRegex();

    internal static ProductSku FromTrustedSource(string value) => new(value);
}

public class ProductSkuJsonConverter : JsonConverter<ProductSku>
{
    public override ProductSku Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? string.Empty;
        return ProductSku.FromTrustedSource(value);
    }

    public override void Write(Utf8JsonWriter writer, ProductSku value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}