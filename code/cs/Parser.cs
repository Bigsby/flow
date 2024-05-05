using System.Text.Json;

internal static class Parser
{
    private record struct PuzzlePosition(string X, string Y, string Type);

    private record struct PuzzleColour(int X1, int Y1, int X2, int Y2);

    private class PuzzleConfiguration//(string Name, string Subtitle, PuzzlePosition[] Positions, PuzzleColour[] Colours);
    {
        public string? Name { get; set; }
        public string? Subtitle { get; set; }
        public PuzzlePosition[] Positions { get; set; } = [];
        public PuzzleColour[] Colours { get; set; } = [];
    }

    private static (int, int) ReadPositions(string input)
    {
        if (input.Contains('-'))
        {
            var split = input.Trim().Split('-');
            return (int.Parse(split[0]), int.Parse(split[1]));
        }
        return (int.Parse(input), int.Parse(input));
    }

    private static Walls ParseType(string input)
    {
        var result = Walls.NONE;
        foreach (var type in input.Split("|"))
            result |= (Walls)Enum.Parse(typeof(Walls), type.Trim());
        return result;
    }

    private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
                positions[new Point(x, y)] = ParseType(position.Type);
        }
        foreach (var colour in configuration.Colours)
            colours.Add((new Point(colour.X1, colour.Y1), new Point(colour.X2, colour.Y2)));
        var maxX = positions.Keys.Max(p => p.X) + 1;
        var maxY = positions.Keys.Max(p => p.Y) + 1;

        return new Puzzle(configuration.Name ?? "", configuration.Subtitle ?? "", positions.AsReadOnly(), colours.ToArray(), maxX, maxY);
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
                    index.ToId(),
                    name,
                    1 + (index * 30),
                    (index + 1) * 30)
                ).ToArray(),
            GroupCounting.Reset => pack.Groups.Select(
                (name, index) => new Group(
                    index.ToId(),
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