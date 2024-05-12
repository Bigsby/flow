using System.Numerics;
using System.Runtime.InteropServices;

public record struct Point(double X, double Y, bool Vertical = true)
{
    public static Point Up = new(0, -1);
    public static Point Down = new(0, 1);
    public static Point Left = new(-1, 0);
    public static Point Right = new(1, 0);
    public static Point[] Directions = [Left, Up, Right, Down];

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator *(Point a, int factor) => new(a.X * factor, a.Y * factor);

    public static implicit operator Point(Complex c) => new(c.Real, c.Imaginary);
    public readonly bool Equals(Point other)
        => other.X == X && other.Y == Y && other.Vertical == Vertical;
    public override readonly int GetHashCode()
        => ((int)X << 3) ^ ((int)Y << 2) ^ (Vertical ? 1 : 0);
    public override readonly string ToString()
        => $"{X},{Y}{(!Vertical ? ",h" : "")}";
    
    public static Point Parse(string value)
    {
        var split = value.Split(',');
        return new Point(
            int.Parse(split[0]),
            int.Parse(split[1]),
            !(split.Length == 3 && split[2] != "h")
        );
    }

    public Point RotateLeft()
        => new(-Y, X);
    
    public Point RotateRight()
        => new(Y, -X);
}