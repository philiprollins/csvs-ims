using System.Text.Json;
using System.Text.Json.Serialization;
using Library;

namespace Application.Features.Product.ValueObjects;

public class ProductName
{
    public string Value { get; private set; }

    private ProductName(string value) => Value = value;

    public static Result<ProductName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail<ProductName>("name", "Product name cannot be empty");
        if (value.Length > 200)
            return Result.Fail<ProductName>("name", "Product name cannot exceed 200 characters");

        return Result.Ok(new ProductName(value.Trim()));
    }

    public override bool Equals(object? obj) => obj is ProductName other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator string(ProductName name) => name.Value;
    public override string ToString() => Value;

    internal static ProductName FromTrustedSource(string value) => new(value);
}

public class ProductNameJsonConverter : JsonConverter<ProductName>
{
    public override ProductName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? string.Empty;
        return ProductName.FromTrustedSource(value);
    }

    public override void Write(Utf8JsonWriter writer, ProductName value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}