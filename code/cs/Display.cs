using System.Numerics;
using static System.Console;

internal static class Display
{
    private static Complex UPLEFT = new Complex(-.5, -.5);
    private static Complex UPRIGHT = new Complex(.5, -.5);
    private static Complex DOWNLEFT = new Complex(-.5, .5);
    private static Complex DOWNRIGHT = new Complex(.5, .5);
    private static Complex LEFT = new Complex(-.5, 0);
    private static Complex RIGHT = new Complex(.5, 0);
    private static Complex UP = new Complex(0, -.5);
    private static Complex DOWN = new Complex(0, .5);

    private static (Complex, Walls)[] VERTICAL = [
        (RIGHT, Walls.LEFT),
        (LEFT, Walls.RIGHT)];

    private static (Complex, Walls)[] HORIZONTAL = [
        (DOWN, Walls.UP),
        (UP, Walls.DOWN)
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

    public static void Print(string text = "")
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
        Print($"level {puzzle.Name} {puzzle.SubTitle} ({puzzle.Colours.Count()})");
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
                        var hasLeft = puzzle.HasWall(position + DOWNLEFT, Walls.UP) || puzzle.HasWall(position + UPLEFT, Walls.DOWN);
                        var hasRight = puzzle.HasWall(position + DOWNRIGHT, Walls.UP) || puzzle.HasWall(position + UPRIGHT, Walls.DOWN);
                        var hasUp = puzzle.HasWall(position + UPLEFT, Walls.RIGHT) || puzzle.HasWall(position + UPRIGHT, Walls.LEFT);
                        var hasDown = puzzle.HasWall(position + DOWNLEFT, Walls.RIGHT) || puzzle.HasWall(position + DOWNRIGHT, Walls.LEFT);
                        var lines = 
                            (hasLeft ? 1 : 0) * (int)Walls.LEFT + 
                            (hasRight ? 1 : 0) * (int)Walls.RIGHT + 
                            (hasUp ? 1 : 0) * (int)Walls.UP + 
                            (hasDown ? 1 : 0) * (int)Walls.DOWN;
                        c = $"{BORDERS[(Walls)lines]}";
                        if (hasLeft && (puzzle.HasPosition(position + UPRIGHT) ^ puzzle.HasPosition(position + DOWNRIGHT)))
                            padding = "\u2500";
                        if (hasRight && (puzzle.HasPosition(position + UPRIGHT) || puzzle.HasPosition(position + DOWNRIGHT)))
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
                        if (solution.TryGetValue(puzzle.NormalizePosition(position + LEFT), out var left)
                            && solution.TryGetValue(puzzle.NormalizePosition(position + RIGHT), out var right)
                            && left == right)
                            c = GetColourRectangle(left, false);
                        if (solution.TryGetValue(puzzle.NormalizePosition(position + UP), out var up)
                            && solution.TryGetValue(puzzle.NormalizePosition(position + DOWN), out var down)
                            && up == down)
                            c = GetColourRectangle(up, true);
                    }
                }
                Write($"{c}{padding}");
            }
            WriteLine();
        }
    }

    public static string SelectItem(string header, IdNameRecord[] list)
    {
        Console.Clear();
        ListObjects(header, list);
        Print();
        while (true)
        {
            Write("Select option: ");
            var input = ReadLine();
            if (input == "b" || input == "q")
                return input;
            input = input?.PadLeft(2, '0');
            var selectedItem = list.FirstOrDefault(i => i.Id == input);
            if (null != selectedItem)
                return selectedItem.Id;
            Error($"Item not found with Id: '{input}'");
        }
    }

    public static void ListObjects(string header, IdNameRecord[] list)
    {
        Print($"{header}: ");
        foreach (var item in list)
            Print($"{item.Id}: {item.Name}");
    }

    public static string GetPuzzleId(string header, int start, int end)
    {
        Print($"{header} ({start}-{end})");
        while (true)
        {
            Write("Select puzzle: ");
            var input = ReadLine();
            if (int.TryParse(input, out var id) && id >= start && id <= end)
                return input?.PadLeft(3, '0') ?? "";
            Error($"'{input}' not valid");
        }
    }
}