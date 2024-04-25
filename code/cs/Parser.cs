using System.Numerics;

internal static class Parser
{
    public static (int, int) ReadPositions(string input)
    {
        if (input.Contains('-'))
        {
            var split = input.Trim().Split('-');
            return (int.Parse(split[0]), int.Parse(split[1]));
        }
        return (int.Parse(input), int.Parse(input));
    }

    public static Complex ReadEnd(string input)
    {
        var split = input.Trim().Split(',');
        return new Complex(int.Parse(split[0]), int.Parse(split[1]));
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