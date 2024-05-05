using System.Diagnostics;
using System.Numerics;

public record struct Point(double X, double Y)
{
    public static Point Up = new(0, -1);
    public static Point Down = new(0, 1);
    public static Point Left = new(-1, 0);
    public static Point Right = new(1, 0);
    public static Point[] Directions = [Left, Up, Right, Down];

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator *(Point a, int factor) => new(a.X * factor, a.Y * factor);

    // public static explicit operator Complex(Point p) => new(p.X, p.Y);
    public static implicit operator Point(Complex c) => new(c.Real, c.Imaginary);
    public readonly bool Equals(Point other)
        => other.X == X && other.Y == Y;
    public override readonly int GetHashCode()
        => ((int)X << 2) ^ (int)Y;
    public override readonly string ToString()
        => $"{X}, {Y}";
}