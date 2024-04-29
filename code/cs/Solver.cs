using System.Numerics;
using ISolution = System.Collections.Generic.IDictionary<System.Numerics.Complex, int>;
using Solution = System.Collections.Generic.Dictionary<System.Numerics.Complex, int>;

internal static class Solver
{
    record struct ColourState(int Colour, bool Complete, Complex Head, Complex End);
    record struct Move(Complex Next, Complex Previous);
    record struct ColourMoves(int Colour, IEnumerable<Move> Moves);

    private static (Complex, Walls)[] DIRECTIONS = [
        (new Complex(-1, 0), Walls.LEFT),
        (new Complex(0, -1), Walls.UP),
        (new Complex(1, 0), Walls.RIGHT),
        (new Complex(0, 1), Walls.DOWN),
    ];

    private static Walls GetDirectionWall(Complex direction)
        => DIRECTIONS.First(d => d.Item1 == direction).Item2;

    public static Walls GetNeighbour(this Puzzle puzzle, Complex position, Complex direction)
    {
        var neighbour = puzzle.NormalizePosition(position + direction);
        if (puzzle.Positions.TryGetValue(neighbour, out var type))
            return type;
        return 0;
    }

    private static IEnumerable<Complex> GetNeighbours(this Puzzle puzzle, Complex position)
    {
        foreach (var (direction, wall) in DIRECTIONS)
        {
            if (puzzle.Positions[position].HasFlag(wall))
                continue;
            var neighbour = position + direction;
            var actualNeighbour = new Complex((neighbour.Real + puzzle.MaxX) % puzzle.MaxX, (neighbour.Imaginary + puzzle.MaxY) % puzzle.MaxY);
            if (puzzle.Positions.ContainsKey(actualNeighbour)) yield return actualNeighbour;
        }
    }

    private static bool HasSameColourNeighbour(this Puzzle puzzle, ISolution solution, Complex position, Complex previous, Complex end, int colour)
        => puzzle.GetNeighbours(position).Any(n => n != previous && n != end && solution.ContainsKey(n) && solution[n] == colour);

    private static IEnumerable<Move> GetPossibleMoves(this Puzzle puzzle, ISolution solution, int colour, Complex head, Complex end)
    {
        var result = new List<Move>();
        foreach (var neighbour in puzzle.GetNeighbours(head))
        {
            if (solution.ContainsKey(neighbour))
                continue;
            if (!puzzle.HasSameColourNeighbour(solution, neighbour, head, end, colour))
                result.Add(new(neighbour, head));
        }
        return result;
    }

    private static ColourMoves[] GetPossibleMoves(this Puzzle puzzle, ISolution solution, IEnumerable<ColourState> colours)
    {
        var result = new List<ColourMoves>();
        foreach (var (colour, complete, head, end) in colours)
        {
            if (complete)
                continue;
            result.Add(new(colour, puzzle.GetPossibleMoves(solution, colour, head, end)));
            result.Add(new(colour, puzzle.GetPossibleMoves(solution, colour, end, head)));
        }
        return result.ToArray();
    }

    private static ISolution Clone(this ISolution solution)
        => solution.ToDictionary(pair => pair.Key, pair => pair.Value);

    private static IList<ColourState> Clone(this IList<ColourState> list)
        => list.Select(c => c with { }).ToList();

    // private static Complex GetMoveDirection(Complex next, Complex previous)
    // {
    //     if (next.Real == previous.Real)
    //         return new Complex(0, next.Imaginary - previous.Imaginary);
    //     return next.Real - previous.Real;
    // }

    private static Complex GetDirection(this Move move)
        => move.Next.Real == move.Previous.Real ?
            new Complex(0, move.Next.Imaginary - move.Previous.Imaginary)
            :
            move.Next.Real - move.Previous.Real;

    private static ColourMoves[] GetCorners(this Puzzle puzzle, ColourMoves[] colourMoves, ISolution solution)
    {
        var result = new List<ColourMoves>();
        foreach (var (colour, moves) in colourMoves)
            foreach (var move in moves)
            {
                var direction = move.GetDirection();
                var wall = GetDirectionWall(direction);
                var position = puzzle.Positions[move.Next];
                if (position.HasFlag(wall))
                    switch (wall)
                    {
                        case Walls.UP:
                        case Walls.DOWN:
                            if (position.HasFlag(Walls.LEFT) ^ position.HasFlag(Walls.RIGHT))
                                result.Add(new(colour, [move]));
                            break;
                        case Walls.LEFT:
                        case Walls.RIGHT:
                            if (position.HasFlag(Walls.UP) ^ position.HasFlag(Walls.DOWN))
                                result.Add(new(colour, [move]));
                            break;

                    }
            }
        return result.ToArray();
    }

    private static void MakeMove(this Puzzle puzzle, ISolution currentSolution, IList<ColourState> currentColours, int colour, Move move)
    {
        var (next, previous) = move;
        currentSolution[next] = colour;
        var colourState = currentColours[colour];
        var otherEnd = previous == colourState.Head ? colourState.End : colourState.Head;
        currentColours[colour] = colourState with
        {
            Complete = next == otherEnd || puzzle.GetNeighbours(next).Any(n => n == otherEnd),
            Head = next,
            End = otherEnd
        };
    }

    public static Complex NormalizePosition(this Puzzle puzzle, Complex position)
        => new Complex((position.Real + puzzle.MaxX) % puzzle.MaxX, (position.Imaginary + puzzle.MaxY) % puzzle.MaxY);

    private static int GetDistanceToSameColour(this Puzzle puzzle, ISolution solution, int colour, Complex position, Complex direction)
    {
        var distance = 0;
        while (true)
        {
            if (puzzle.Positions[position].HasFlag(GetDirectionWall(direction)))
                return -1;
            position = puzzle.NormalizePosition(position + direction);
            if (!puzzle.Positions.ContainsKey(position))
                return -1;
            distance++;
            if (solution.ContainsKey(position))
            {
                if (solution[position] == colour)
                    return distance;
                return -1;
            }
        }
    }

    private static Walls InverseWall(this Walls wall)
        => wall switch
        {
            Walls.DOWN => Walls.UP,
            Walls.UP => Walls.DOWN,
            Walls.LEFT => Walls.RIGHT,
            Walls.RIGHT => Walls.LEFT,
            _ => Walls.NONE
        };

    private static readonly IReadOnlyDictionary<Complex, Complex[]> UTURNDIRECTIONS = new Dictionary<Complex, Complex[]>
    {
        { Complex.One, [Complex.ImaginaryOne, -Complex.ImaginaryOne] },
        { -Complex.One, [Complex.ImaginaryOne, -Complex.ImaginaryOne] },
        { Complex.ImaginaryOne, [Complex.One, -Complex.One] },
        { -Complex.ImaginaryOne, [Complex.One, -Complex.One] },
    };

    private static int GetDistanceToWall(this Puzzle puzzle, ISolution solution, Complex position, Complex direction)
    {
        var distance = 0;
        while (true)
        {
            if (puzzle.Positions[position].HasFlag(GetDirectionWall(direction)))
                return distance;
            position = puzzle.NormalizePosition(position + direction);
            distance++;
            if (solution.ContainsKey(position))
                return -1;
        }
    }

    private static bool CreatesDeadEnd(this Puzzle puzzle, ISolution solution, Move move)
    {
        var direction = move.GetDirection();
        var directionWall = GetDirectionWall(direction);
        if (!puzzle.Positions[move.Next].HasFlag(directionWall))
            return false;

        foreach (var otherDirection in UTURNDIRECTIONS[direction])
        {
            var distance = puzzle.GetDistanceToWall(solution, move.Next, otherDirection);
            switch (distance)
            {
                case 0:
                    return false;
                case 1:
                    var nextPosition = puzzle.NormalizePosition(move.Next + otherDirection);
                    if (puzzle.Positions.TryGetValue(nextPosition, out var wall) && wall.HasFlag(directionWall))
                        return true;
                    break;
            }
        }
        return false;
    }

    private static bool CreatesUTurn(this Puzzle puzzle, ISolution solution, int colour, Move move)
    {
        var direction = move.GetDirection();
        foreach (var otherDirection in UTURNDIRECTIONS[direction])
        {
            var diagonal = puzzle.NormalizePosition(move.Next - direction + otherDirection);
            if (solution.TryGetValue(diagonal, out int value) && value != colour)
                continue;
            var distance = puzzle.GetDistanceToSameColour(solution, colour, move.Next, otherDirection);
            switch (distance)
            {
                case -1:
                    continue;
                case 1:
                    return true;
                case 2:
                    var positionsToCheck = new[]
                    {
                        puzzle.NormalizePosition(move.Next - direction + otherDirection),
                        puzzle.NormalizePosition(move.Next - direction + otherDirection * 2)
                    };
                    if (positionsToCheck.All(position => solution.TryGetValue(position, out var value) && value == colour))
                        return true;
                    break;
                case 3:
                    var thisSide = puzzle.NormalizePosition(move.Next - direction + otherDirection);
                    var otherSide = puzzle.NormalizePosition(thisSide + otherDirection);
                    var hitWall = GetDirectionWall(otherDirection);
                    if (puzzle.Positions.TryGetValue(thisSide, out var thisWall) && !thisWall.HasFlag(hitWall)
                        && 
                        puzzle.Positions.TryGetValue(otherSide, out var otherWall) && !otherWall.HasFlag(hitWall.InverseWall()))
                        return true;
                    break;
                case 4:
                    var apex = puzzle.NormalizePosition(move.Next + direction + otherDirection * 2);
                    if (!solution.ContainsKey(apex))
                        return true;
                    break;
            }
        }
        return false;
    }

    public static IReadOnlyDictionary<Complex, int> Solve(this Puzzle puzzle)
    {
        Display.Clear();
        var initialSolution = new Solution();
        var initialColours = new List<ColourState>();
        foreach (var (start, end, colour) in puzzle.Colours.Select((c, i) => (c.Item1, c.Item2, i)))
        {
            initialSolution[start] = colour;
            initialSolution[end] = colour;
            initialColours.Add(new(colour, false, start, end));
        }
        var queue = new Stack<(ISolution, IList<ColourState>, (int, Complex)[])>();
        queue.Push((initialSolution, initialColours, []));
        while (queue.Any())
        {
            var (currentSolution, currentColours, currentRejects) = queue.Pop();
            Display.GoToTop();
            puzzle.Print(currentSolution);

            var mandatoryMoves = false;

            var possibleMoves = puzzle.GetPossibleMoves(currentSolution, currentColours);

            if (possibleMoves.Any(move => !move.Moves.Any() && !currentColours[move.Colour].Complete))
                continue;

            var singleMoves = possibleMoves.Where(p => p.Moves.Count() == 1)
                .ToArray();

            foreach (var (colour, moves) in singleMoves)
            {
                mandatoryMoves = true;
                puzzle.MakeMove(currentSolution, currentColours, colour, moves.First());
            }

            var multipleMoves = possibleMoves.Except(singleMoves).ToArray();
            var corners = puzzle.GetCorners(multipleMoves, currentSolution);

            foreach (var (colour, moves) in corners)
            {
                mandatoryMoves = true;
                puzzle.MakeMove(currentSolution, currentColours, colour, moves.First());
            }

            if (mandatoryMoves)
            {
                if (currentColours.All(c => c.Complete))
                    return currentSolution.AsReadOnly();
                queue.Push((currentSolution, currentColours, currentRejects));
                continue;
            }

            if (currentColours.All(c => c.Complete))
                return currentSolution.AsReadOnly();

            foreach (var (colour, moves) in possibleMoves)
            {
                foreach (var move in moves)
                {
                    if (currentRejects.Contains((colour, move.Next)))
                        continue;
                    var newSolution = currentSolution.Clone();
                    var newColours = currentColours.Clone();
                    var newRejects = currentRejects.Select(r => r).ToList();
                    puzzle.MakeMove(newSolution, newColours, colour, move);
                    if (puzzle.CreatesUTurn(newSolution, colour, move))
                    {
                        newRejects.Add((colour, move.Next));
                        // Display.Error("UTURN >>>>>>>>>>>>");
                        // puzzle.Print(newSolution, move.Next);
                        // Display.Key();
                        continue;
                    }
                    if (puzzle.CreatesDeadEnd(newSolution, move))
                    {
                        newRejects.Add((colour, move.Next));
                        // Display.Error("DEAD END >>>>>>>>>>>>");
                        // puzzle.Print(newSolution, move.Next);
                        // Display.Key();
                        continue;
                    }
                    queue.Push((newSolution, newColours, newRejects.ToArray()));
            // Display.GoToTop();
            // puzzle.Print(currentSolution);
            //         Display.Print("New ++++++++++++++++");
            //         puzzle.Print(newSolution, move.Next);
            //         Display.Key();
                }
            }
        }
        throw new Exception("Solution not found");
    }
}