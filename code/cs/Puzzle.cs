using System.Numerics;

[Flags]
enum Walls
{
    NONE = 0,
    LEFT = 1 << 0,
    UP = 1 << 1,
    RIGHT = 1 << 2,
    DOWN = 1 << 3,
    BRIDGE = 1 << 4,
}

record struct Puzzle(
    string Name,
    string SubTitle,
    IReadOnlyDictionary<Complex, Walls> Positions,
    IEnumerable<(Complex, Complex)> Colours,
    double MaxX,
    double MaxY);

class PuzzleBuilder
{
    private string _name = string.Empty;
    private string _subTitle = string.Empty;
    private double _maxX = 0;
    private double _maxY = 0;
    private IDictionary<Complex, Walls> _positions = new Dictionary<Complex, Walls>();
    private IList<(Complex, Complex)> _colours = new List<(Complex, Complex)>();

    public Puzzle Build()
        => new(_name, _subTitle, _positions.AsReadOnly(), _colours.ToArray(), _maxX, _maxY);

    public PuzzleBuilder SetName(string name)
    {
        _name = name;
        return this;
    }

    public PuzzleBuilder SetSubTitle(string subTitle)
    {
        _subTitle = subTitle;
        return this;
    }

    public PuzzleBuilder AddPosition(Complex position, Walls type)
    {
        _positions[position] = type;
        _maxX = Math.Max(_maxX, position.Real);
        _maxY = Math.Max(_maxY, position.Imaginary);
        return this;
    }

    public PuzzleBuilder AddColour(Complex end1, Complex end2)
    {
        _colours.Add((end1, end2));
        return this;
    }
}