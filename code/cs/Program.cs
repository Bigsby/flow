using System.Numerics;
using System.Linq;
using static System.Console;

record struct Puzzle(
    string Name,
    string SubTitle,
    IReadOnlyDictionary<Complex, Walls> Positions,
    IEnumerable<(Complex, Complex)> Colours,
    double MaxX,
    double MaxY);

class PuzzleBuilder
{
    private string _name = string.Empty;
    private string _subTitle = string.Empty;
    private double _maxX = 0;
    private double _maxY = 0;
    private IDictionary<Complex, Walls> _positions = new Dictionary<Complex, Walls>();
    private IList<(Complex, Complex)> _colours = new List<(Complex, Complex)>();

    public Puzzle Build()
        => new(_name, _subTitle, _positions.AsReadOnly(), _colours.ToArray(), _maxX, _maxY);

    public PuzzleBuilder SetName(string name)
    {
        _name = name;
        return this;
    }

    public PuzzleBuilder SetSubTitle(string subTitle)
    {
        _subTitle = subTitle;
        return this;
    }

    public PuzzleBuilder AddPosition(Complex position, Walls type)
    {
        _positions[position] = type;
        _maxX = Math.Max(_maxX, position.Real);
        _maxY = Math.Max(_maxY, position.Imaginary);
        return this;
    }

    public PuzzleBuilder AddColour(Complex end1, Complex end2)
    {
        _colours.Add((end1, end2));
        return this;
    }
}

[Flags]
enum Walls
{
    LEFT = 1 << 0,
    UP = 1 << 1,
    RIGHT = 1 << 2,
    DOWN = 1 << 3,
    BRIDGE = 1 << 4,
}

internal static class Program
{
    static int[] COLOURS = [196, 21, 28, 226, 208, 51, 206, 88, 90];
    static (Complex, Walls)[] DIRECTIONS = [
        (new Complex(-1, 0), Walls.LEFT),
        (new Complex(0, -1), Walls.UP),
        (new Complex(1, 0), Walls.RIGHT),
        (new Complex(0, 1), Walls.DOWN),
    ];

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

    private static async Task<Puzzle> ReadPuzzleFile(string filePath)
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

    private static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> filter)
    {
        if (null == source)
            throw new ArgumentNullException(nameof(source));
        var index = 0;
        foreach (var item in source)
            if (filter(item))
                return index;
            else
                index++;
        return -1;
    }

    private static int GetColour(this Puzzle puzzle, Complex position)
        => puzzle.Colours.IndexOf(ends => ends.Item1 == position || ends.Item2 == position);

    private static Walls GetNeighbour(this Puzzle puzzle, Complex position, Complex direction)
    {
        var neighbour = position + direction;
        if (puzzle.Positions.TryGetValue(neighbour, out var type))
            return type;
        return 0;
    }

    private static IEnumerable<Complex> GetNeighbours(this Puzzle puzzle, Complex position)
    {
        foreach (var (direction, wall) in DIRECTIONS)
        {
            if (puzzle.Positions[position].HasFlag(wall))
                continue;
            var neighour = position + direction;
            if (puzzle.Positions.ContainsKey(neighour))
                yield return neighour;
        }
    }

    private static IReadOnlyDictionary<Complex, int> Solve(this Puzzle puzzle)
    {
        var initialSolution = new Dictionary<Complex, int>();
        var initialColours = new List<(int, bool, Complex, Complex)>();
        foreach (var (start, end, colour) in puzzle.Colours.Select((c, i) => (c.Item1, c.Item2, i)))
        {
            initialSolution[start] = colour;
            initialSolution[end] = colour;
            initialColours.Add((colour, false, start, end));
        }
        var queue = new Queue<(Dictionary<Complex, int>, List<(int, bool, Complex, Complex)>)>();
        queue.Enqueue((initialSolution, initialColours));
        while (queue.Any())
        {
            var (currentSolution, currentColours) = queue.Dequeue();
            // puzzle.Print(currentSolution); 
            // WriteLine(string.Join("\n", currentColours.ToArray()));
            // ReadLine();
            foreach (var (colour, complete, head, end) in currentColours)
            {
                if (complete)
                    continue;
                var pathFound = false;
                foreach (var neighbour in puzzle.GetNeighbours(head))
                {
                    if (currentSolution.ContainsKey(neighbour) && neighbour != end)
                        continue;
                    
                    pathFound = true;
                    var newSolution = currentSolution.ToDictionary(pair => pair.Key, pair => pair.Value);
                    var newColours = currentColours.Select(c => c).ToList();
                    newSolution[neighbour] = colour;
                    newColours[colour] = (colour, neighbour == end, neighbour, end);
                    if (newColours.All(c => c.Item2))
                        return newSolution.AsReadOnly();
                    queue.Enqueue((newSolution, newColours));
                }
                if (pathFound)
                    break;
            }

        }
        throw new Exception("Solution not found");
    }

    private static string GetColourDot(int colour)
        => colour == -1 ?
        "\u25E6"
        :
        $"\x1b[38;5;{COLOURS[colour]}m\u25CF\x1b[0m";

    private static (Complex, Walls)[] VERTICAL = [
        (new Complex(.5, 0), Walls.LEFT),
        (new Complex(-.5, 0), Walls.RIGHT)];

    private static (Complex, Walls)[] HORIZONTAL = [
        (new Complex(0, .5), Walls.UP),
        (new Complex(0, -.5), Walls.DOWN)
    ];

    private static void Print(this Puzzle puzzle, IReadOnlyDictionary<Complex, int>? solution = default)
    {
        WriteLine($"{puzzle.Name} {puzzle.SubTitle} {puzzle.MaxX} {puzzle.MaxY}");
        for (double y = 0; y < puzzle.MaxY * 2 + 3; y++)
        {
            for (double x = 0; x < puzzle.MaxX * 2 + 3; x++)
            {
                var c = " ";
                var position = new Complex(
                    (x - 1) / 2,
                    (y - 1) / 2);
                if (x % 2 == 1 && y % 2 == 1) // position
                {
                    if (solution?.TryGetValue(position, out var colour) ?? false)
                        c = GetColourDot(colour);
                    else
                        c = GetColourDot(GetColour(puzzle, position));
                }
                else // wall
                {
                    if (x % 2 == 0 && y % 2 == 0)
                        c = " ";
                    if (y % 2 == 1)
                    {
                        foreach (var (direction, wall) in VERTICAL)
                            if (puzzle.GetNeighbour(position, direction).HasFlag(wall))
                                c = "\u2503";
                    }
                    else if (x % 2 == 1)
                        foreach (var (direction, wall) in HORIZONTAL)
                            if (puzzle.GetNeighbour(position, direction).HasFlag(wall))
                                c = "\u2501";
                }
                Write($"{c} ");
            }
            WriteLine();
        }
    }

    private static async Task Main(string[] args)
    {
        if (args.Length != 1)
            throw new ArgumentException("Please add (only) puzzle file path");

        var filePath = args[0];
        var puzzle = await ReadPuzzleFile(filePath);
        puzzle.Print();
        var solution = puzzle.Solve();
        puzzle.Print(solution);
    }
}