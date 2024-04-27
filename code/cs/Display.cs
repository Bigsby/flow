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

    public static string GetColourDot(int colour)
        => colour == -1 ?
        "\u25E6"
        :
        $"\x1b[38;5;{COLOURS[colour]}m\u25CF\x1b[0m";
    
    public static void Error(string message)
        => Console.Error.WriteLine($"\x1b[38;5;196m{message}\x1b[0m");

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

    public static void Write(string text)
        => WriteLine(text);

    public static void Key()
        => ReadLine();

    public static int GetColour(this Puzzle puzzle, Complex position)
        => puzzle.Colours.IndexOf(ends => ends.Item1 == position || ends.Item2 == position);

    public static void Print(this Puzzle puzzle, IDictionary<Complex, int>? solution = default, bool clear = false, bool step = false)
    {
        if (clear)
            Clear();
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
                        c = " "; if (y % 2 == 1)
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
                System.Console.Write($"{c} ");
            }
            WriteLine();
        }
        if (step)
            ReadLine();
    }
}