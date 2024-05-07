using System.Text.Json;
using System.Text.Json.Serialization;

public class PuzzleTypeJsonConverter : JsonConverter<PuzzleType>
{
    public override PuzzleType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => (PuzzleType)Enum.Parse(typeof(PuzzleType), reader.GetString()!, true);

    public override void Write(Utf8JsonWriter writer, PuzzleType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString().ToLower());
}

public class WallsJsonConverter : JsonConverter<Walls>
{
    private static Walls ParseType(string input)
    {
        var result = Walls.NONE;
        foreach (var type in input.Split("|"))
            result |= (Walls)Enum.Parse(typeof(Walls), type.Trim());
        return result;
    }

    public override Walls Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value!.Contains('|'))
            return ParseType(value);
        return Enum.Parse<Walls>(value);
    }

    public override void Write(Utf8JsonWriter writer, Walls value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}

public class PointJsonConverter : JsonConverter<Point>
{
    public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()!;
        var split = value.Split(',');
        return new(double.Parse(split[0]), double.Parse(split[1]));
    }

    public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        => writer.WriteStringValue($"{value.X},{value.Y}");
}

public class SolutionJsonConverter : JsonConverter<Solution>
{
    private static Point ParsePoint(string value)
    {
        var split = value.Split(',');
        return new(double.Parse(split[0]), double.Parse(split[1]));
    }
    public override Solution? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new Solution();
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Token type {reader.TokenType} instead of {nameof(JsonTokenType.StartObject)}");
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return result;
            
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Token type {reader.TokenType} instead of {nameof(JsonTokenType.PropertyName)}");
            var point = ParsePoint(reader.GetString()!);
            reader.Read();
            if (reader.TryGetInt32(out var colour))
                result.Add(point, colour);
        }
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Solution value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (point, colour) in value)
        {
            writer.WritePropertyName(point.ToString());
            writer.WriteNumberValue(colour);
        }
        writer.WriteEndObject();
    }
}