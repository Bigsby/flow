using System.Numerics;
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

    private static Complex ReadEnd(string input)
    {
        var split = input.Trim().Split(',');
        return new Complex(int.Parse(split[0]), int.Parse(split[1]));
    }

    private static Walls ParseType(string input)
    {
        var result = Walls.NONE;
        foreach (var type in input.Split("|"))
            result |= (Walls)Enum.Parse(typeof(Walls), type.Trim());
        return result;
    }

    private static Puzzle FromConfiguration(PuzzleConfiguration? configuration)
    {
        if (null == configuration)
            throw new ArgumentNullException(nameof(configuration));
        var positions = new Dictionary<Complex, Walls>();
        var colours = new List<(Complex, Complex)>();
        foreach (var position in configuration.Positions)
        {
            var (x1, x2) = ReadPositions(position.X);
            var (y1, y2) = ReadPositions(position.Y);
            for (var x = x1; x <= x2; x++)
                for (var y = y1; y <= y2; y++)
                positions[new Complex(x, y)] = ParseType(position.Type);
        }
        foreach (var colour in configuration.Colours)
            colours.Add((new Complex(colour.X1, colour.Y1), new Complex(colour.X2, colour.Y2)));
        var maxX = (int)positions.Keys.Max(p => p.Real);
        var maxY = (int)positions.Keys.Max(p => p.Imaginary);

        return new Puzzle(configuration.Name ?? "", configuration.Subtitle ?? "", positions.AsReadOnly(), colours.ToArray(), maxX, maxY);
    }

    public static async Task<Puzzle> ReadPuzzleJson(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($@"'{filePath}' does not exist");
        var json = await File.ReadAllTextAsync(filePath);
        var configuration = JsonSerializer.Deserialize<PuzzleConfiguration>(
            json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return FromConfiguration(configuration);
    }


    public static async Task<Puzzle> ReadPuzzleFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($@"{filePath} does not exist");

        var builder = new PuzzleBuilder();
        var stage = 0;
        foreach (var line in await File.ReadAllLinesAsync(filePath))
        {
            var stripped = line.Trim();
            switch (stage)
            {
                case 0:
                    builder.SetName(stripped);
                    stage++;
                    break;
                case 1:
                    builder.SetSubTitle(stripped);
                    stage++;
                    break;
                case 2:
                    stage++;
                    break;
                case 3:
                    if (string.IsNullOrWhiteSpace(stripped))
                    {
                        stage++;
                        continue;
                    }
                    var split = line.Split(' ');
                    var (positions, type) = (split[0], split[1]);
                    var positionSplits = positions.Split(',');
                    var (x1, x2) = ReadPositions(positionSplits[0]);
                    var (y1, y2) = ReadPositions(positionSplits[1]);
                    for (var x = x1; x <= x2; x++)
                        for (var y = y1; y <= y2; y++)
                        builder.AddPosition(new Complex(x, y), (Walls)int.Parse(type));
                    break;
                case 4:
                    if (!string.IsNullOrWhiteSpace(stripped))
                    {
                        var endsSplit = line.Split(' ');
                        var (end1, end2) = (endsSplit[0], endsSplit[1]);
                        builder.AddColour(ReadEnd(end1), ReadEnd(end2));
                    }
                    break;
            }
        }
        return builder.Build();
    }
}