global using ISolution = System.Collections.Generic.IDictionary<Point, int>;
global using Solution = System.Collections.Generic.Dictionary<Point, int>;

[Flags]
public enum Walls
{
    NONE = 0,
    LEFT = 1 << 0, // 1
    UP = 1 << 1, // 2
    RIGHT = 1 << 2, // 4
    DOWN = 1 << 3, // 8
    BRIDGE = 1 << 4, // 16
}

record struct Puzzle(
    string Name,
    string SubTitle,
    IReadOnlyDictionary<Point, Walls> Positions,
    IEnumerable<(Point, Point)> Colours,
    int MaxX,
    int MaxY,
    Solution? Solution);
