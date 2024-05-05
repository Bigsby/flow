using System.Collections.Frozen;
using System.ComponentModel;
using System.Numerics;

internal static class Menu
{
    private static string DirectoryName(this IdNameRecord item)
        => $"{item.Id} - {item.Name}";

    public static async Task SelectGame(Game[] games)
    {
        var game = SelectItem("Games", games);
        var headerItems = new Stack<string>();
        var header = () => string.Join(" > ", headerItems.Reverse());
        while (null != game)
        {
            headerItems.Push(game.Name);
            var section = SelectItem(header(), game.Sections);
            while (null != section)
            {
                headerItems.Push(section.Name);
                var pack = SelectItem(header(), section.Packs);
                while (null != pack)
                {
                    headerItems.Push(pack.Name);
                    var group = SelectItem(header(), pack.GetGroups());
                    while (null != group)
                    {
                        headerItems.Push(group.Name);
                        var puzzleId = Display.SelectPuzzleId(header(), group.Start, group.End);
                        while (puzzleId != -1)
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
                                var worker = new BackgroundWorker
                                {
                                    WorkerSupportsCancellation = true,
                                    WorkerReportsProgress = false
                                };
                                worker.DoWork += (s, e) => e.Result = puzzle.Solve();
                                worker.RunWorkerCompleted += (s, e) =>
                                {
                                    if (!e.Cancelled)
                                    {
                                        var solution = e.Result as ISolution;
                                        puzzle.Print(solution);
                                    }
                                };
                                worker.RunWorkerAsync();
                                Display.StartProgress();
                                while (worker.IsBusy ^ worker.CancellationPending)
                                    if (Display.EscapePressed() && worker.IsBusy)
                                    {
                                        worker.CancelAsync();
                                        Display.StopProgress();
                                        Display.Print("Solving cancelled!");
                                    }
                                    else
                                        Display.TickProgress();
                                Display.StopProgress();
                            }
                            catch (Exception ex)
                            {
                                Display.Error(ex.Message);
                            }
                            Display.Key();
                            puzzleId = Display.SelectPuzzleId(header(), group.Start, group.End);
                        }
                        headerItems.Pop();
                        group = SelectItem(header(), pack.GetGroups());
                    }
                    headerItems.Pop();
                    pack = SelectItem(header(), section.Packs);
                }
                headerItems.Pop();
                section = SelectItem(header(), game.Sections);
            }
            headerItems.Pop();
            game = SelectItem("Games", games);
        }
    }

    private static T? SelectItem<T>(string header, T[] items) where T : IdNameRecord
    {
        var id = Display.SelectItem(header, items);
        if (id == "q" || id == "b" || string.IsNullOrEmpty(id))
            return default;
        var item = items.Single(i => i.Id == id);
        if (null == item)
            throw new ArgumentException($"'{id}' is not known.");
        return item;
    }
}