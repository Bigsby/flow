using System.Numerics;
using static System.Console;

internal static class Display
{
    private static readonly Complex LEFT = new(-.5, 0);
    private static readonly Complex RIGHT = new(.5, 0);
    private static readonly Complex UP = new(0, -.5);
    private static readonly Complex DOWN = new(0, .5);
    private static readonly Complex UPLEFT = UP + LEFT;
    private static readonly Complex UPRIGHT = UP + RIGHT;
    private static readonly Complex DOWNLEFT = DOWN + LEFT;
    private static readonly Complex DOWNRIGHT = DOWN + RIGHT;

    private static readonly (Point, Walls)[] VERTICAL = [
        (RIGHT, Walls.LEFT),
        (LEFT, Walls.RIGHT)];

    private static readonly (Point, Walls)[] HORIZONTAL = [
        (DOWN, Walls.UP),
        (UP, Walls.DOWN)
    ];

    static readonly int[] COLOURS = [196, 21, 28, 226, 208, 51, 206, 88, 90, 255];

    public static string GetColourDot(int colour, bool highlight = false)
        => colour == -1 ?
        "\u25E6"
        :
        $"\x1b[1m\x1b[38;5;{COLOURS[colour]}m{(highlight ? "\x1b[48;5;47m" : "")}\u25CF\x1b[0m";

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

    public static ConsoleKey Key()
        => ReadKey(true).Key;

    public static bool EscapePressed()
        => KeyAvailable && Key() == ConsoleKey.Escape;

    private static int progressIndex = 0;
    private static string[] progressText = ["|", "/", "-", "\\"];

    public static void StartProgress()
        => Write(progressText[progressIndex]);

    public static void TickProgress()
    {
        progressIndex++;
        if (progressIndex == progressText.Length)
            progressIndex = 0;
        SetCursorPosition(0, CursorTop);
        Write(progressText[progressIndex]);
    }

    public static void StopProgress()
    {
        SetCursorPosition(0, CursorTop);
        Write(" ");
        SetCursorPosition(0, CursorTop);
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

    private static int GetColour(this Puzzle puzzle, Point position)
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
        { Walls.BRIDGE, '\u253C' }
    };

    private static bool HasWall(this Puzzle puzzle, Point position, Walls wall)
        => puzzle.Positions.TryGetValue(position, out var value) && value.HasFlag(wall);

    private static bool HasPosition(this Puzzle puzzle, Point position)
        => puzzle.Positions.ContainsKey(position);
    
    private static void PrintSquare(Puzzle puzzle, Solution? solution, Point? move)
    {
        if (solution.HasValue)
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
                    if (puzzle.Positions[position] == Walls.BRIDGE)
                        c = $"{BORDERS[Walls.BRIDGE]}";
                    else if (solution?.TryGetColour(position, out var colour) ?? false)
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
                        if (hasRight && (puzzle.HasPosition(position + UPRIGHT) ^ puzzle.HasPosition(position + DOWNRIGHT)))
                            padding = "\u2500";
                    }
                    if (y % 2 == 1)
                    {
                        foreach (var (direction, wall) in VERTICAL)
                            if (puzzle.HasWall(position + direction, wall))
                                c = "\u2502";
                    }
                    else if (x % 2 == 1)
                        foreach (var (direction, wall) in HORIZONTAL)
                            if (puzzle.HasWall(position + direction, wall))
                            {
                                c = "\u2500";
                                padding = "\u2500";
                            }
                    if (c == " " && solution.HasValue)
                    {
                        if ((solution?.TryGetColour(puzzle.NormalizePosition(position + LEFT), out var left) ?? false)
                            && (solution?.TryGetColour(puzzle.NormalizePosition(position + RIGHT), out var right) ?? false)
                            && left == right)
                            c = GetColourRectangle(left, false);
                        if ((solution?.TryGetColour(puzzle.NormalizePosition(position + UP), out var up) ?? false)
                            && (solution?.TryGetColour(puzzle.NormalizePosition(position + DOWN), out var down) ?? false)
                            && up == down)
                            c = GetColourRectangle(up, true);
                    }
                }
                Write($"{c}{padding}");
            }
            WriteLine();
        }
    }
    
    private static void PrintHexagonal(Puzzle puzzle, Solution? solution, Point? move)
    {
        Print("Printing hexagonal");
    }

    public static void Print(this Puzzle puzzle, Solution? solution = default, Point? move = default)
    {
        switch (puzzle.Type)
        {
            case PuzzleType.Square:
                PrintSquare(puzzle, solution, move);
                break;
            case PuzzleType.Hexagonal:
                PrintHexagonal(puzzle, solution, move);
                break;
        }
    }

    public static string SelectItem(string header, IdNameRecord[] list)
    {
        Clear();
        Print($"{header}: ");
        var selectedIndex = 0;
        ListObjects(list, selectedIndex);
        while (true)
        {
            var key = ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.DownArrow or ConsoleKey.J:
                    selectedIndex++;
                    break;
                case ConsoleKey.UpArrow or ConsoleKey.K:
                    selectedIndex--;
                    break;
                case ConsoleKey.Escape or ConsoleKey.Q or ConsoleKey.Backspace:
                    return "";
                case ConsoleKey.Enter:
                    return list[selectedIndex].Id;
            }
            if (selectedIndex < 0)
                selectedIndex = list.Length - 1;
            if (selectedIndex == list.Length)
                selectedIndex = 0;
            var (_, top) = GetCursorPosition();
            SetCursorPosition(0, top - list.Length);
            ListObjects(list, selectedIndex);
        }
    }

    private static string ToMenuItem(this IdNameRecord item)
        => $"{item.Id} - {item.Name}";

    private static void ListObjects(IdNameRecord[] list, int selectedIndex)
    {
        for (var index = 0; index < list.Length; index++)
            if (index == selectedIndex)
                WriteLine($"\x1b[38;5;0m\x1b[48;5;255m{list[index].ToMenuItem()}\x1b[0m");
            else
                WriteLine($"\x1b[38;5;255m\x1b[48;5;0m{list[index].ToMenuItem()}\x1b[0m");
    }

    public static int SelectPuzzleId(string header, int start, int end)
    {
        var selected = start;
        while (true)
        {
            Clear();
            Print($"{header}: ");
            for (var index = start; index <= end; index++)
            {
                if (index == selected)
                    Write($"\x1b[38;5;0m\x1b[48;5;255m{index,3}\x1b[0m");
                else
                    Write($"\x1b[38;5;255m\x1b[48;5;0m{index,3}\x1b[0m");
                if (index % 5 == 0)
                    WriteLine();
                else
                    Write(" ");
            }
            switch (ReadKey(true).Key)
            {
                case ConsoleKey.UpArrow or ConsoleKey.K:
                    if (selected >= start + 5)
                        selected -= 5;
                    else
                        selected += 25;
                    break;
                case ConsoleKey.DownArrow or ConsoleKey.J:
                    if (selected <= end - 5)
                        selected += 5;
                    else
                        selected -= 25;
                    break;
                case ConsoleKey.LeftArrow or ConsoleKey.H:
                    if (selected % 5 != 1)
                        selected--;
                    else
                        selected += 4;
                    break;
                case ConsoleKey.RightArrow or ConsoleKey.L:
                    if (selected % 5 != 0)
                        selected++;
                    else
                        selected -= 4;
                    break;
                case ConsoleKey.Enter:
                    return selected;
                case ConsoleKey.Escape or ConsoleKey.Backspace:
                    return -1;
            }
        }
    }
}