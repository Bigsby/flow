internal static class Solver
{
    record struct ColourState(int Colour, bool Complete, Point Head, Point End);
    record struct Move(Point Next, Point Previous, Point Direction);
    record struct ColourMoves(int Colour, IEnumerable<Move> Moves);

    private static (Point, Walls)[] DIRECTIONS = [
        (Point.Left, Walls.LEFT),
        (Point.Up, Walls.UP),
        (Point.Right, Walls.RIGHT),
        (Point.Down, Walls.DOWN),
    ];

    private static Walls GetDirectionWall(Point direction)
        => DIRECTIONS.First(d => d.Item1 == direction).Item2;

    public static Walls GetNeighbour(this Puzzle puzzle, Point position, Point direction)
    {
        var neighbour = puzzle.NormalizePosition(position + direction);
        if (puzzle.Positions.TryGetValue(neighbour, out var type))
            return type;
        return 0;
    }

    private static IEnumerable<(Point Neighbour, Point Direction)> GetNeighbours(this Puzzle puzzle, Point position)
    {
        foreach (var (direction, wall) in DIRECTIONS)
        {
            if (puzzle.Positions[position].HasFlag(wall))
                continue;
            var neighbour = puzzle.NormalizePosition(position + direction);
            if (puzzle.Positions.ContainsKey(neighbour)) yield return (neighbour, direction);
        }
    }

    private static bool HasSameColourNeighbour(this Puzzle puzzle, Solution solution, Point position, Point previous, Point end, int colour)
        => puzzle.GetNeighbours(position)
            .Any(n =>
                n.Neighbour != previous
                && n.Neighbour != end
                && solution.Contains(n.Neighbour)
                && solution.HasColour(n.Neighbour, colour));

    private static IEnumerable<Move> GetPossibleMoves(this Puzzle puzzle, Solution solution, int colour, Point head, Point end)
    {
        var result = new List<Move>();
        foreach (var (neighbour, direction) in puzzle.GetNeighbours(head))
        {
            if (solution.Contains(neighbour))
                continue;
            if (!puzzle.HasSameColourNeighbour(solution, neighbour, head, end, colour))
                result.Add(new(neighbour, head, direction));
        }
        return result;
    }

    private static ColourMoves[] GetPossibleMoves(this Puzzle puzzle, Solution solution, IEnumerable<ColourState> colours)
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

    private static IList<ColourState> Clone(this IList<ColourState> list)
        => list.Select(c => c with { }).ToList();

    private static ColourMoves[] GetCorners(this Puzzle puzzle, ColourMoves[] colourMoves, Solution solution)
    {
        var result = new List<ColourMoves>();
        foreach (var (colour, moves) in colourMoves)
            foreach (var move in moves)
            {
                var direction = move.Direction;
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

    private static void MakeMove(this Puzzle puzzle, Solution currentSolution, IList<ColourState> currentColours, int colour, Move move)
    {
        var (next, previous, direction) = move;
        currentSolution.Add(colour, next);
        var colourState = currentColours[colour];
        var otherEnd = previous == colourState.Head ? colourState.End : colourState.Head;
        currentColours[colour] = colourState with
        {
            Complete = next == otherEnd || puzzle.GetNeighbours(next).Any(n => n.Item1 == otherEnd),
            Head = next,
            End = otherEnd
        };
    }

    public static Point NormalizePosition(this Puzzle puzzle, Point position)
        => new((position.X + puzzle.MaxX) % puzzle.MaxX, (position.Y + puzzle.MaxY) % puzzle.MaxY);

    private static int GetDistanceToSameColour(this Puzzle puzzle, Solution solution, int colour, Point position, Point direction)
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
            if (solution.Contains(position))
            {
                if (solution.HasColour(position, colour))
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

    private static readonly IReadOnlyDictionary<Point, Point[]> UTURNDIRECTIONS = new Dictionary<Point, Point[]>
    {
        { Point.Right, [Point.Down, Point.Up] },
        { Point.Left, [Point.Down, Point.Up] },
        { Point.Down, [Point.Right, Point.Left] },
        { Point.Up, [Point.Right, Point.Left] },
    };

    private static int GetDistanceToWall(this Puzzle puzzle, Solution solution, Point position, Point direction)
    {
        var distance = 0;
        while (true)
        {
            if (puzzle.Positions[position].HasFlag(GetDirectionWall(direction)))
                return distance;
            position = puzzle.NormalizePosition(position + direction);
            distance++;
            if (solution.Contains(position))
                return -1;
        }
    }

    private static bool CreatesDeadEnd(this Puzzle puzzle, Solution solution, Move move)
    {
        var direction = move.Direction;
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

    private static bool CreatesUTurn(this Puzzle puzzle, Solution solution, int colour, Move move)
    {
        var direction = move.Direction;
        foreach (var otherDirection in UTURNDIRECTIONS[direction])
        {
            var diagonal = puzzle.NormalizePosition(move.Next - direction + otherDirection);
            if (solution.HasColour(diagonal, colour))
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
                    if (positionsToCheck.All(position => solution.HasColour(position, colour)))
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
                    if (!solution.Contains(apex))
                        return true;
                    break;
            }
        }
        return false;
    }

    private static bool CreatesImpossibleState(this Puzzle puzzle, Solution solution, int colour, Move move)
        =>
            puzzle.CreatesUTurn(solution, colour, move)
            ||
            puzzle.CreatesDeadEnd(solution, move);

    private static bool IsSolved(this Puzzle puzzle, Solution solution, IEnumerable<ColourState> colours)
        =>  puzzle.Positions.Keys.All(solution.Contains) && colours.All(c => c.Complete);

    public static Solution Solve(this Puzzle puzzle, CancellationToken token)
    {
        var initialSolution = new Solution(puzzle.Colours.Count());
        var initialColours = new List<ColourState>();
        foreach (var (start, end, colour) in puzzle.Colours.Select((c, i) => (c.Item1, c.Item2, i)))
        {
            initialSolution.Add(colour, start);
            initialSolution.Add(colour, end);
            initialColours.Add(new(colour, false, start, end));
        }
        var queue = new Stack<(Solution, IList<ColourState>, (int, Point)[])>();
        queue.Push((initialSolution, initialColours, []));
        while (queue.Count != 0 && !token.IsCancellationRequested)
        {
            var (currentSolution, currentColours, currentRejects) = queue.Pop();

            var possibleMoves = puzzle.GetPossibleMoves(currentSolution, currentColours);

            if (possibleMoves.Any(move => !move.Moves.Any() && !currentColours[move.Colour].Complete))
                continue;

            var singleMoves = possibleMoves.Where(p => p.Moves.Count() == 1)
                .ToArray();

            var mandatoryMovesMade = false;

            foreach (var (colour, moves) in singleMoves)
            {
                mandatoryMovesMade = true;
                puzzle.MakeMove(currentSolution, currentColours, colour, moves.First());
            }

            var multipleMoves = possibleMoves.Except(singleMoves).ToArray();
            var corners = puzzle.GetCorners(multipleMoves, currentSolution);

            foreach (var (colour, moves) in corners)
            {
                mandatoryMovesMade = true;
                puzzle.MakeMove(currentSolution, currentColours, colour, moves.First());
            }

            if (puzzle.IsSolved(currentSolution, currentColours))
                return currentSolution;

            if (mandatoryMovesMade)
            {
                queue.Push((currentSolution, currentColours, currentRejects));
                continue;
            }

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
                    if (puzzle.CreatesImpossibleState(newSolution, colour, move))
                    {
                        newRejects.Add((colour, move.Next));
                        continue;
                    }
                    queue.Push((newSolution, newColours, newRejects.ToArray()));
                    // puzzle.Print(newSolution, move.Next);
                    // Display.Key();
                }
            }
        }
        throw new Exception("Solution not found");
    }
}