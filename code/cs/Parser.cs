using System.Text.Json;

internal static class Parser
{
    record GamesData(Game[] Games);

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
        _jsonOptions.Converters.Add(new PuzzleTypeJsonConverter());
    }

    private record struct PuzzlePosition(string Positions, Walls Type);

    private record struct PuzzleColour(int? X1, int? Y1, int? X2, int? Y2, Point? E1, Point? E2);

    private record PuzzleConfiguration(
        string Name, 
        string Subtitle, 
        PuzzlePosition[] Positions, 
        PuzzleColour[] Colours, 
        Solution? Solution,
        PuzzleType? Type = PuzzleType.Square);

    public static string SerializeSolution(Solution solution)
        => JsonSerializer.Serialize(solution, _jsonOptions);
    
    public static string SerializePuzzle(Puzzle puzzle)
        => JsonSerializer.Serialize(puzzle, _jsonOptions);

    public static (int Start, int End) ReadPositions(string input)
    {
        if (input.Contains('-'))
        {
            var split = input.Trim().Split('-');
            return (int.Parse(split[0]), int.Parse(split[1]));
        }
        return (int.Parse(input), int.Parse(input));
    }

    public static (int XStart, int XEnd, int YStart, int YEnd) ReadPositionsGroup(string input)
    {
        var split = input.Split(',');
        var xs = ReadPositions(split[0]);
        var ys = ReadPositions(split[1]);
        return (xs.Start, xs.End, ys.Start, ys.End);
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
        var data = await ParseJsonFile<GamesData>(filePath);
        return data?.Games;
    }

    private static Puzzle FromConfiguration(PuzzleConfiguration? configuration)
    {
        if (null == configuration)
            throw new ArgumentNullException(nameof(configuration));
        var positions = new Dictionary<Point, Walls>();
        var colours = new List<(Point, Point)>();
        foreach (var position in configuration.Positions)
        {
            var points = ReadPositionsGroup(position.Positions);
            for (var x = points.XStart; x <= points.XEnd; x++)
                for (var y = points.YStart; y <= points.YEnd; y++)
                positions[new Point(x, y)] = position.Type;
        }
        foreach (var colour in configuration.Colours)
        if (colour.X1.HasValue && colour.Y1.HasValue
            && colour.X2.HasValue && colour.Y2.HasValue)
            colours.Add((new Point(colour.X1.Value, colour.Y1.Value),
                        new Point(colour.X2.Value, colour.Y2.Value)));
        else
            colours.Add((colour.E1!.Value, colour.E2!.Value));
        var maxX = positions.Keys.Max(p => p.X) + 1;
        var maxY = positions.Keys.Max(p => p.Y) + 1;

        return new Puzzle(
            configuration.Name ?? "", 
            configuration.Subtitle ?? "", positions.AsReadOnly(), 
            colours.ToArray(), (int)maxX, (int)maxY, configuration.Solution, configuration.Type);
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
                    (index + 1).ToId(),
                    name,
                    1 + (index % 2 == 1 ? 30 : 0),
                    (index % 2 == 1 ? 1 : 2) * 30)
                ).ToArray(),
            _ => throw new NotImplementedException()
        };
}