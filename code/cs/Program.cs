using System.Collections.Frozen;

internal static class Program
{

    private static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Display.Error("Please add (only) puzzle file path");
            return;
        }

        var filePath = args[0];
        try 
        {
            var puzzle = await Parser.ReadPuzzleJson(filePath);
            puzzle.Print();
            var solution = puzzle.Solve();
            
            Display.Write("Solution:");
            puzzle.Print(solution.ToFrozenDictionary());
        } 
        catch (FileNotFoundException ex)
        {
            Display.Error(ex.Message);
        }
        catch (Exception ex)
        {
            Display.Error(ex.Message);
        }
    }
}