using System.Diagnostics;
using System.Text.Json;

internal static class Menu
{
    private static string DirectoryName(this IdNameRecord item)
        => $"{item.Id} - {item.Name}";

    public static async Task SelectGame(Game[] games)
    {
        var headerItems = new Stack<string>();
        var header = () => string.Join(" > ", headerItems.Reverse());
        while (TrySelectItem("Games", games, out var game))
        {
            headerItems.Push(game!.Name);
            while (TrySelectItem(header(), game!.Sections, out var section))
            {
                headerItems.Push(section!.Name);
                while (TrySelectItem(header(), section!.Packs, out var pack))
                {
                    headerItems.Push(pack!.Name);
                    while (TrySelectItem(header(), pack!.GetGroups(), out var group))
                    {
                        headerItems.Push(group!.Name);
                        var puzzleId = -1; ;
                        while ((puzzleId = Display.SelectPuzzleId(header(), group.Start, group.End)) != -1)
                        {
                            try
                            {
                                var puzzleFileName = puzzleId.ToString().PadLeft(3, '0');
                                var filePath = Path.Combine(
                                    Directory.GetCurrentDirectory(),
                                        "puzzles",
                                        game.Id,
                                        section.DirectoryName(),
                                        pack.DirectoryName(),
                                        group.DirectoryName(),
                                        $"{puzzleFileName}.json");
                                var puzzle = await Parser.ReadPuzzleJson(filePath);
                                puzzle.Print();
                                if (puzzle.Solution.HasValue)
                                {
                                    Display.Print("Stored solution.");
                                    Display.Print(JsonSerializer.Serialize(puzzle.Solution));
                                    puzzle.Print(puzzle.Solution);
                                }
                                else
                                {
                                    var source = new CancellationTokenSource();
                                    var task = new Task<Solution>(() => puzzle.Solve(source.Token), source.Token);
                                    var watch = Stopwatch.StartNew();
                                    task.Start();
                                    Display.StartProgress();
                                    while (!task.IsCompleted)
                                        if (Display.EscapePressed())
                                            source.Cancel();
                                        else
                                            Display.TickProgress();
                                    Display.StopProgress();
                                    watch.Stop();
                                    Display.Print($"Time: {(double)watch.ElapsedTicks / 100 / TimeSpan.TicksPerSecond:f7}");
                                    if (task.IsCompletedSuccessfully)
                                    {
                                        puzzle.Print(task.Result);
                                        Display.Print(Parser.SerializeSolution(task.Result));
                                    }
                                    else if (task.IsFaulted)
                                    {
                                        Display.Error(task.Exception.Message);
                                        Display.Print(task.Exception.StackTrace ?? "");
                                    } else if (task.IsCanceled)
                                        Display.Error("Solving interrupted");
                                }
                            }
                            catch (Exception ex)
                            {
                                Display.Error(ex.Message);
                                Display.Print(ex.StackTrace ?? "");
                            }
                            Display.Key();
                        }
                        headerItems.Pop();
                    }
                    headerItems.Pop();
                }
                headerItems.Pop();
            }
            headerItems.Pop();
        }
    }

    private static bool TrySelectItem<T>(string header, T[] items, out T? result) where T : IdNameRecord
    {
        var id = Display.SelectItem(header, items);
        if (id == "q" || id == "b" || string.IsNullOrEmpty(id))
        {
            result = default;
            return false;
        }
        result = items.SingleOrDefault(i => i.Id == id);
        return default != result;
    }
}