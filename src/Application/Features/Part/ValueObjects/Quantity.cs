using System.Text.Json;
using System.Text.Json.Serialization;
using Library;

namespace Application.Features.Part.ValueObjects;

public class Quantity
{
    public int Value { get; private set; }

    private Quantity(int value) => Value = value;

    public static Result<Quantity> Create(int value)
    {
        if (value > 999999)
            return Result.Fail<Quantity>("quantity", "Quantity exceeds maximum allowed (999,999)");
        if (value < -999999)
            return Result.Fail<Quantity>("quantity", "Quantity is below minimum allowed (-999,999)");

        return Result.Ok(new Quantity(value));
    }

    public Result<Quantity> Add(Quantity other)
    {
        return Create(Value + other.Value);
    }

    public Result<Quantity> Subtract(Quantity other)
    {
        return Create(Value - other.Value);
    }

    public override bool Equals(object? obj) => obj is Quantity other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator int(Quantity quantity) => quantity.Value;
    public override string ToString() => Value.ToString();

    internal static Quantity FromTrustedSource(int value) => new(value);
}

public class QuantityJsonConverter : JsonConverter<Quantity>
{
    public override Quantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetInt32();
        return Quantity.FromTrustedSource(value);
    }

    public override void Write(Utf8JsonWriter writer, Quantity value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}