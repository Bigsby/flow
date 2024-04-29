using System.Collections.Frozen;

internal static class Menu
{
    private static string DirectoryName(this IdNameRecord item)
        => $"{item.Id} - {item.Name}";

    public static async Task SelectGame(Game[] games)
    {
        var game = SelectItem("Games", games);
        if (null == game)
            return;
        var header = $"{game.Name}";
        var section = SelectItem(header, game.Sections);
        if (null == section)
            return;
        header += $" > {section.Name}";
        var pack = SelectItem(header, section.Packs);
        if (null == pack)
            return;
        header += $" > {pack.Name}";
        var group = SelectItem(header, pack.Groups);
        if (null == group)
            return;
        header += $" > {group.Name}";
        var puzzleId = Display.GetPuzzleId(header, group.Start, group.End);
        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(), 
            "puzzles",
            game.Id, 
            section.DirectoryName(), 
            pack.DirectoryName(), 
            group.DirectoryName(), 
            $"{puzzleId}.json");
        var puzzle = await Parser.ReadPuzzleJson(filePath);
        puzzle.Print();
        var solution = puzzle.Solve();
        puzzle.Print(solution.ToFrozenDictionary());
    }

    private static T? SelectItem<T>(string header, T[] items) where T : IdNameRecord
    {
        var id = Display.SelectItem(header, items);
        if (id == "q")
            return default;
        var item = items.Single(i => i.Id == id);
        if (null == item)
            throw new ArgumentException($"'{id}' is not known.");
        return item;
    }
}