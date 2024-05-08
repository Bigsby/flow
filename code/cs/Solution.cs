readonly struct Solution(int colourCount)
{
    private readonly HashSet<Point>[] _colours = Enumerable.Range(0, colourCount).Select(_ => new HashSet<Point>()).ToArray();
    private readonly int colourCount = colourCount;

    public void Add(int colour, Point point)
        => _colours[colour].Add(point);

    public bool Contains(Point point)
        => _colours.Any(colour => colour.Contains(point));
    
    public bool HasColour(Point point, int colour)
        => _colours[colour].Contains(point);

    public bool TryGetColour(Point point, out int colour)
    {
        foreach (var (colourPoints, index) in _colours.Select((c, i) => (c, i)))
            if (colourPoints.Contains(point))
            {
                colour = index;
                return true;
            }
        colour = 0;
        return false;
    }

    public Solution CloneSolution()
    {
        var clone = new Solution(colourCount);
        for (var index = 0; index < colourCount; index++)
            clone._colours[index] = [.. _colours[index]];
        return clone;
    }

    public IEnumerable<Point>[] GetColours()
        => _colours.Select(c => c.ToArray()).ToArray();
    
    public static bool AreEquals(Solution a, Solution b)
        => a.colourCount != b.colourCount
        &&
        Enumerable.Range(0, a.colourCount).All(colour => !a._colours[colour].Except(b._colours[colour]).Any() && !b._colours[colour].Except(a._colours[colour]).Any());
}