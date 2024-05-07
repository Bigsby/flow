using System.Text.Json;
using System.Text.Json.Serialization;

class PuzzleTypeJsonConverter : JsonConverter<PuzzleType>
{
    public override PuzzleType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => (PuzzleType)Enum.Parse(typeof(PuzzleType), reader.GetString()!, true);

    public override void Write(Utf8JsonWriter writer, PuzzleType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString().ToLower());
}

class WallsJsonConverter : JsonConverter<Walls>
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

class PointJsonConverter : JsonConverter<Point>
{
    public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()!;
        var split = value.Split(',');
        return new(double.Parse(split[0]), double.Parse(split[1]), split.Length == 3 && split[2] == "h");
    }

    public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        => writer.WriteStringValue($"{value.X},{value.Y}");
}

class SolutionJsonConverter : JsonConverter<Solution>
{
    public override Solution Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"{reader.TokenType} not expected for Solution start");
        var colours = new List<List<Point>>();
        var colourIndex = -1;
        reader.Read();
        do {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
                colourIndex++;
                colours.Add(new List<Point>());
                continue;
            }
            var point = Point.Parse(reader.GetString()!);
            colours[colourIndex].Add(point);
            reader.Read();

            if (reader.TokenType == JsonTokenType.EndArray)
                reader.Read();
        } while (reader.TokenType != JsonTokenType.EndArray);
        var result = new Solution(colours.Count);
        foreach (var (colour, index) in colours.Select((c, i) => (c, i)))
            foreach (var point in colour)
                result.Add(index, point);
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Solution value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var colourPoints in value.GetColours())
        {
            writer.WriteStartArray();
            foreach(var point in colourPoints)
                JsonSerializer.Serialize(writer, point, options);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}