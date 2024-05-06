using System.Text.Json;
using System.Text.Json.Serialization;

internal static class Parser
{
    private static JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    static Parser()
    {
        _jsonOptions.Converters.Add(new PointJsonConverter());
        _jsonOptions.Converters.Add(new SolutionJsonConverter());
        _jsonOptions.Converters.Add(new WallsJsonConverter());
    }

    private record struct PuzzlePosition(string X, string Y, Walls Type);

    private record struct PuzzleColour(int X1, int Y1, int X2, int Y2);

    private record PuzzleConfiguration(string Name, string Subtitle, PuzzlePosition[] Positions, PuzzleColour[] Colours, Solution? Solution);

    public static string SerializeSolution(Solution solution)
        => JsonSerializer.Serialize(solution, _jsonOptions);
    
    public static string SerializePuzzle(Puzzle puzzle)
        => JsonSerializer.Serialize(puzzle, _jsonOptions);

    private static (int, int) ReadPositions(string input)
    {
        if (input.Contains('-'))
        {
            var split = input.Trim().Split('-');
            return (int.Parse(split[0]), int.Parse(split[1]));
        }
        return (int.Parse(input), int.Parse(input));
    }

    private async static Task<T?> ParseJsonFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($@"'{filePath}' does not exist");
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<T>(
            json, _jsonOptions);
    }

    public static async Task<Game[]?> GetGamesData(string filePath)
    {
        var data = await ParseJsonFile<Game[]>(filePath);
        return data;
    }

    private static Puzzle FromConfiguration(PuzzleConfiguration? configuration)
    {
        if (null == configuration)
            throw new ArgumentNullException(nameof(configuration));
        var positions = new Dictionary<Point, Walls>();
        var colours = new List<(Point, Point)>();
        foreach (var position in configuration.Positions)
        {
            var (x1, x2) = ReadPositions(position.X);
            var (y1, y2) = ReadPositions(position.Y);
            for (var x = x1; x <= x2; x++)
                for (var y = y1; y <= y2; y++)
                positions[new Point(x, y)] = position.Type;
        }
        foreach (var colour in configuration.Colours)
            colours.Add((new Point(colour.X1, colour.Y1), new Point(colour.X2, colour.Y2)));
        var maxX = positions.Keys.Max(p => p.X) + 1;
        var maxY = positions.Keys.Max(p => p.Y) + 1;

        return new Puzzle(
            configuration.Name ?? "", 
            configuration.Subtitle ?? "", positions.AsReadOnly(), 
            colours.ToArray(), (int)maxX, (int)maxY, configuration.Solution);
    }

    public static async Task<Puzzle> ReadPuzzleJson(string filePath)
    {
        var configuration = await ParseJsonFile<PuzzleConfiguration>(filePath);
        return FromConfiguration(configuration);
    }

    private static string ToId(this int id)
        => id.ToString("00");

    public static Group[] GetGroups(this Pack pack)
        => pack.Counting switch
        {
            GroupCounting.Continuous => pack.Groups.Select(
                (name, index) => new Group(
                    (index + 1).ToId(),
                    name,
                    1 + (index * 30),
                    (index + 1) * 30)
                ).ToArray(),
            GroupCounting.Reset => pack.Groups.Select(
                (name, index) => new Group(
                    (index + 1).ToId(),
                    name,
                    1, 30)
                ).ToArray()
            ,
            GroupCounting.Reset2 => pack.Groups.Select(
                (name, index) => new Group(
                    index.ToId(),
                    name,
                    1 + (index % 2 == 1 ? 30 : 0),
                    (index % 2 == 1 ? 1 : 2) * 30)
                ).ToArray(),
            _ => throw new NotImplementedException()
        };
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