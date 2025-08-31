using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Library;

namespace Application.Features.Part.ValueObjects;

public partial class PartSku
{
    public string Value { get; private set; }

    private PartSku(string value) => Value = value;

    public static Result<PartSku> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail<PartSku>("sku", "Part SKU cannot be empty");
        if (value.Length > 50)
            return Result.Fail<PartSku>("sku", "Part SKU cannot exceed 50 characters");
        if (!IsValidSkuFormat(value))
            return Result.Fail<PartSku>("sku", "Part SKU must contain only alphanumeric characters and hyphens");

        return Result.Ok(new PartSku(value.ToUpperInvariant()));
    }

    private static bool IsValidSkuFormat(string value) =>
        MyRegex().IsMatch(value);

    public override bool Equals(object? obj) => obj is PartSku other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator string(PartSku sku) => sku.Value;
    public override string ToString() => Value;
    [GeneratedRegex(@"^[A-Za-z0-9\-]+$")]
    private static partial Regex MyRegex();

    internal static PartSku FromTrustedSource(string value) => new(value);
}

public class PartSkuJsonConverter : JsonConverter<PartSku>
{
    public override PartSku Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? string.Empty;
        return PartSku.FromTrustedSource(value);
    }

    public override void Write(Utf8JsonWriter writer, PartSku value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}