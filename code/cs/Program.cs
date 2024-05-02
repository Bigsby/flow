using System.Collections.Frozen;

internal static class Program
{

    private static async Task Main(string[] args)
    {
        Console.CursorVisible = false;
        if (args.Length == 2 && args[0] == "-d")
        {
            var data = await Parser.GetGamesData(args[1]);
            if (null == data)
            {
                Display.Error("Unable to read data");
                return;
            }
            await Menu.SelectGame(data);
            Console.CursorVisible = true;
            return;
        }
        if (args.Length != 1)
        {
            Display.Error("Please add (only) puzzle file path");
            return;
        }

        var input = args[0];
        try 
        {
            var puzzle = await Parser.ReadPuzzleJson(input);
            puzzle.Print();
            var solution = puzzle.Solve();
            
            Display.Print("Solution:");
            puzzle.Print(solution.ToFrozenDictionary());
        } 
        catch (FileNotFoundException ex)
        {
            Display.Error(ex.Message);
        }
        catch (Exception ex)
        {
            Display.Error(ex.Message);
            Display.Print(ex.StackTrace ?? "");
        }
        Console.CursorVisible = true;
    }
}