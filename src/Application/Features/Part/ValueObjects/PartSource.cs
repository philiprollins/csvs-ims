using System.Text.Json;
using System.Text.Json.Serialization;
using Library;

namespace Application.Features.Part.ValueObjects;

public class PartSource
{
    public string Name { get; private set; }
    public string Uri { get; private set; }
    
    private PartSource(string name, string uri)
    {
        Name = name;
        Uri = uri;
    }
    
    public static Result<PartSource> Create(string name, string uri)
    {
        var errors = new Dictionary<string, string>();
        
        if (string.IsNullOrWhiteSpace(name))
            errors["sourceName"] = "Source name cannot be empty";
        else if (name.Length > 100)
            errors["sourceName"] = "Source name cannot exceed 100 characters";
        
        
        if (string.IsNullOrWhiteSpace(uri))
            errors["sourceUri"] = "Source URI cannot be empty";
        else if (uri.Length > 200)
            errors["sourceUri"] = "Source URI cannot exceed 200 characters";
        else if (!System.Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            errors["sourceUri"] = "Source URI is not a valid absolute URI";
        
        if (errors.Count > 0)
            return Result.Fail<PartSource>(errors);
        
        return Result.Ok(new PartSource(name.Trim(), uri.Trim()));
    }
    
    public override bool Equals(object? obj) => 
        obj is PartSource other && Name == other.Name && Uri == other.Uri;
    public override int GetHashCode() => HashCode.Combine(Name, Uri);

    internal static PartSource FromTrustedSource(string name, string uri) => new(name, uri);
}

public class PartSourceJsonConverter : JsonConverter<PartSource>
{
    public override PartSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        
        var name = root.GetProperty("Name").GetString() ?? string.Empty;
        var uri = root.GetProperty("Uri").GetString() ?? string.Empty;
        
        return PartSource.FromTrustedSource(name, uri);
    }

    public override void Write(Utf8JsonWriter writer, PartSource value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Name", value.Name);
        writer.WriteString("Uri", value.Uri);
        writer.WriteEndObject();
    }
}