struct Solution(int colourCount)
{
    private readonly HashSet<Point>[] _colours = Enumerable.Range(0, colourCount).Select(_ => new HashSet<Point>()).ToArray();

    public readonly void Add(int colour, Point point)
        => _colours[colour].Add(point);

    public readonly bool Contains(Point point)
        => _colours.Any(colour => colour.Contains(point));
    
    public readonly bool HasColour(Point point, int colour)
        => _colours[colour].Contains(point);

    public readonly bool TryGetColour(Point point, out int colour)
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

    public readonly Solution Clone()
    {
        var clone = new Solution(colourCount);
        for (var index = 0; index < colourCount; index++)
            clone._colours[index] = [.. _colours[index]];
        return clone;
    }

    public readonly IEnumerable<Point>[] GetColours()
        => _colours.Select(c => c.ToArray()).ToArray();
}