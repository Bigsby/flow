using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using static System.Console;

internal static class Display
{
    private static (Complex, Walls)[] VERTICAL = [
        (new Complex(.5, 0), Walls.LEFT),
        (new Complex(-.5, 0), Walls.RIGHT)];

    private static (Complex, Walls)[] HORIZONTAL = [
        (new Complex(0, .5), Walls.UP),
        (new Complex(0, -.5), Walls.DOWN)
    ];

    static int[] COLOURS = [196, 21, 28, 226, 208, 51, 206, 88, 90];

    public static string GetColourDot(int colour, bool highlight = false)
        => colour == -1 ?
        "\u25E6"
        :
        $"\x1b[38;5;{COLOURS[colour]}m{(highlight ? "\x1b[48;5;47m" : "")}\u25CF\x1b[0m";
    
    private static string GetColourRectangle(int colour, bool vertical)
        => colour == -1 ?
        " "
        :
        $"\x1b[38;5;{COLOURS[colour]}m{(vertical ? '\u25AE' : '\u25AC')}\x1b[0m";

    public static void Error(string message)
        => Console.Error.WriteLine($"\x1b[38;5;196m{message}\x1b[0m");

    public static void Clear()
        => Console.Clear();

    public static void GoToTop()
        => Write("\x1b[0;0H");

    public static void Print(string text)
        => WriteLine(text);

    public static void Key()
        => ReadLine();

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
    
    private static IReadOnlyDictionary<Walls, char> BORDERS = new Dictionary<Walls, char>
    {
        { Walls.NONE, ' ' },
        { Walls.LEFT | Walls.DOWN, '\u2510' },
        { Walls.LEFT | Walls.UP, '\u2518' },
        { Walls.LEFT | Walls.UP | Walls.DOWN, '\u2524' },
        { Walls.LEFT | Walls.RIGHT | Walls.UP | Walls.DOWN, '\u253C' },
        { Walls.LEFT | Walls.RIGHT, '\u2500' },
        { Walls.LEFT | Walls.RIGHT | Walls.DOWN, '\u252C' },
        { Walls.RIGHT | Walls.DOWN, '\u250C' },
        { Walls.RIGHT | Walls.UP, '\u2514' },
        { Walls.RIGHT | Walls.UP | Walls.DOWN, '\u251C' },
        { Walls.UP | Walls.DOWN, '\u2502' },
        { Walls.RIGHT, '\u2576' },
        { Walls.LEFT, '\u2574' },
        { Walls.UP, '\u2575' },
        { Walls.DOWN, '\u2577' },
    };

    private static bool HasWall(this Puzzle puzzle, Complex position, Walls wall)
        => puzzle.Positions.TryGetValue(position, out var value) && value.HasFlag(wall);
    
    private static bool HasPosition(this Puzzle puzzle, Complex position)
        => puzzle.Positions.ContainsKey(position);

    public static void Print(this Puzzle puzzle, IDictionary<Complex, int>? solution = default, Complex? move = default)
    {
        Print($"{puzzle.Name} {puzzle.SubTitle} {puzzle.MaxX} {puzzle.MaxY}");
        for (double y = 0; y < puzzle.MaxY * 2 + 1; y++)
        {
            for (double x = 0; x < puzzle.MaxX * 2 + 1; x++)
            {
                var c = " ";
                var padding = " ";
                var position = new Complex(
                    (x - 1) / 2,
                    (y - 1) / 2);
                if (x % 2 == 1 && y % 2 == 1 && puzzle.Positions.ContainsKey(position)) // position
                {
                    if (solution?.TryGetValue(position, out var colour) ?? false)
                        c = GetColourDot(colour, position == move);
                    else
                        c = GetColourDot(GetColour(puzzle, position));
                }
                else // wall
                {
                    if (x % 2 == 0 && y % 2 == 0)
                    {
                        var hasLeft = puzzle.HasWall(position + new Complex(-.5, .5), Walls.UP) || puzzle.HasWall(position + new Complex(-.5, -.5), Walls.DOWN);
                        var hasRight = puzzle.HasWall(position + new Complex(.5, .5), Walls.UP) || puzzle.HasWall(position + new Complex(.5, -.5), Walls.DOWN);
                        var hasUp = puzzle.HasWall(position + new Complex(-.5, -.5), Walls.RIGHT) || puzzle.HasWall(position + new Complex(.5, -.5), Walls.LEFT);
                        var hasDown = puzzle.HasWall(position + new Complex(-.5, .5), Walls.RIGHT) || puzzle.HasWall(position + new Complex(.5, .5), Walls.LEFT);
                        var lines = 
                            (hasLeft ? 1 : 0) * (int)Walls.LEFT + 
                            (hasRight ? 1 : 0) * (int)Walls.RIGHT + 
                            (hasUp ? 1 : 0) * (int)Walls.UP + 
                            (hasDown ? 1 : 0) * (int)Walls.DOWN;
                        c = $"{BORDERS[(Walls)lines]}";
                        if (hasLeft && (puzzle.HasPosition(position + new Complex(.5, -.5)) ^ puzzle.HasPosition(position + new Complex(.5, .5))))
                            padding = "\u2500";
                        if (hasRight && (puzzle.HasPosition(position + new Complex(.5, -.5)) || puzzle.HasPosition(position + new Complex(.5, .5))))
                            padding = "\u2500";
                    }
                    if (y % 2 == 1)
                    {
                        foreach (var (direction, wall) in VERTICAL)
                            if (puzzle.GetNeighbour(position, direction).HasFlag(wall))
                                c = "\u2502";
                    }
                    else if (x % 2 == 1)
                        foreach (var (direction, wall) in HORIZONTAL)
                            if (puzzle.GetNeighbour(position, direction).HasFlag(wall))
                            {
                                c = "\u2500";
                                padding = "\u2500";
                            }
                    if (c == " " && null != solution)
                    {
                        if (solution.TryGetValue(puzzle.NormalizePosition(position + new Complex(-.5, 0)), out var left)
                            && solution.TryGetValue(puzzle.NormalizePosition(position + new Complex(.5, 0)), out var right)
                            && left == right)
                            c = GetColourRectangle(left, false);
                        if (solution.TryGetValue(puzzle.NormalizePosition(position + new Complex(0, -.5)), out var up)
                            && solution.TryGetValue(puzzle.NormalizePosition(position + new Complex(0, .5)), out var down)
                            && up == down)
                            c = GetColourRectangle(up, true);
                    }
                }
                Write($"{c}{padding}");
            }
            WriteLine();
        }
    }
}