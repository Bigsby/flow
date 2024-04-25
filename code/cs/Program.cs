using System.Numerics;
using System.Linq;
using System.Collections.Frozen;
using static System.Console;

internal static class Program
{

    private static async Task Main(string[] args)
    {
        if (args.Length != 1)
            throw new ArgumentException("Please add (only) puzzle file path");

        var filePath = args[0];
        var puzzle = await Parser.ReadPuzzleFile(filePath);
        puzzle.Print();
        var solution = puzzle.Solve();
        WriteLine("Solution:");
        puzzle.Print(solution.ToFrozenDictionary());
    }
}