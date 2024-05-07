global using ISolution = System.Collections.Generic.IDictionary<Point, int>;
global using Solution = System.Collections.Generic.Dictionary<Point, int>;

[Flags]
public enum Walls
{
    NONE = 0,
    LEFT = 1 << 0,
    UP = 1 << 1,
    RIGHT = 1 << 2,
    DOWN = 1 << 3,
    BRIDGE = 1 << 4,
    UPLEFT = 1 << 5,
    UPRIGHT = 1 << 6,
    DOWNLEFT = 1 << 7,
    DOWNRIGHT = 1 << 8
}

public enum PuzzleType
{
    Square,
    Hexagonal
}

record struct Puzzle(
    string Name,
    string SubTitle,
    IReadOnlyDictionary<Point, Walls> Positions,
    IEnumerable<(Point, Point)> Colours,
    int MaxX,
    int MaxY,
    Solution? Solution,
    PuzzleType? Type = PuzzleType.Square);
