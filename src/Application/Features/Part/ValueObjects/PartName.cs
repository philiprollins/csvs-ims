using System.Text.Json;
using System.Text.Json.Serialization;
using Library;

namespace Application.Features.Part.ValueObjects;

public class PartName
{
    public string Value { get; private set; }

    private PartName(string value) => Value = value;

    public static Result<PartName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail<PartName>("name", "Part name cannot be empty");
        if (value.Length > 200)
            return Result.Fail<PartName>("name", "Part name cannot exceed 200 characters");

        return Result.Ok(new PartName(value.Trim()));
    }

    public override bool Equals(object? obj) => obj is PartName other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator string(PartName name) => name.Value;
    public override string ToString() => Value;

    internal static PartName FromTrustedSource(string value) => new(value);
}

public class PartNameJsonConverter : JsonConverter<PartName>
{
    public override PartName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? string.Empty;
        return PartName.FromTrustedSource(value);
    }

    public override void Write(Utf8JsonWriter writer, PartName value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}