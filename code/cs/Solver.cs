internal static class Solver
{
    record struct ColourState(int Colour, bool Complete, Point Head, Point End);
    record struct Move(Point Next, Point Previous, Point Direction);
    record struct ColourMoves(int Colour, Move[] Moves);

    private static readonly (Point Direction, Walls Wall)[] DIRECTIONS = [
        (Point.Left, Walls.LEFT),
        (Point.Up, Walls.UP),
        (Point.Right, Walls.RIGHT),
        (Point.Down, Walls.DOWN),
    ];

    private static Walls GetWall(this Point direction)
        => DIRECTIONS.First(d => d.Direction == direction).Wall;

    private static bool HasPosition(this Puzzle puzzle, Point position)
        => puzzle.Positions.ContainsKey(position);

    private static bool IsFull(this Puzzle puzzle, Solution solution)
        => puzzle.Positions.Keys.All(position => solution.Contains(position));
    
    private static bool PositionHasWall(this Puzzle puzzle, Point position, Walls wall)
        => puzzle.Positions.TryGetValue(position, out var puzzleWall) && puzzleWall.HasFlag(wall);
    
    private static IEnumerable<(Point Neighbour, Point Direction)> GetNeighbours(this Puzzle puzzle, Point position)
        => DIRECTIONS.Where(move => !puzzle.PositionHasWall(position, move.Wall))
            .Select(move => (puzzle.NormalizePosition(position + move.Direction), move.Direction))
            .Where(pair => puzzle.HasPosition(pair.Item1));

    private static bool HasSameColourNeighbour(this Puzzle puzzle, Solution solution, Point position, Point previous, Point end, int colour)
        => puzzle.GetNeighbours(position)
            .Any(n =>
                n.Neighbour != previous
                && n.Neighbour != end
                && solution.Contains(n.Neighbour)
                && solution.HasColour(n.Neighbour, colour));

    private static Move[] GetPossibleMoves(this Puzzle puzzle, Solution solution, int colour, Point head, Point end)
        => [.. puzzle.GetNeighbours(head)
            .Where(move => puzzle.CanGoDirection(solution, head, move.Direction) && !puzzle.HasSameColourNeighbour(solution, move.Neighbour, head, end, colour))
            .Select(move => new Move(move.Neighbour, head, move.Direction))];

    private static ColourMoves[] GetPossibleMoves(this Puzzle puzzle, Solution solution, IEnumerable<ColourState> colours)
        => [.. colours.Where(colour => !colour.Complete)
            .SelectMany(colourState => new ColourMoves[] {
                new(colourState.Colour, puzzle.GetPossibleMoves(solution, colourState.Colour, colourState.Head, colourState.End)),
                new(colourState.Colour, puzzle.GetPossibleMoves(solution, colourState.Colour, colourState.End, colourState.Head))
            })];

    private static IList<ColourState> Clone(this IList<ColourState> list)
        => [.. list.Select(c => c with { })];

    private static ColourMoves[] GetCorners(this Puzzle puzzle, ColourMoves[] colourMoves, Solution solution)
    {
        var result = new List<ColourMoves>();
        foreach (var (colour, moves) in colourMoves)
            foreach (var move in moves)
            {
                var direction = move.Direction;
                var wall = direction.GetWall();
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
        return [.. result];
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
            if (puzzle.PositionHasWall(position, direction.GetWall()))
                return -1;
            position = puzzle.NormalizePosition(position + direction);
            if (!puzzle.HasPosition(position))
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
            if (puzzle.PositionHasWall(position, direction.GetWall()))
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
        var directionWall = direction.GetWall();
        if (!puzzle.PositionHasWall(move.Next, directionWall))
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
                    if (puzzle.PositionHasWall(nextPosition, directionWall))
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
                    var hitWall = otherDirection.GetWall();
                    if (!(puzzle.PositionHasWall(thisSide, hitWall)
                        ||
                        puzzle.PositionHasWall(otherSide, hitWall.InverseWall())))
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

    private static bool CanGoDirection(this Puzzle puzzle, Solution solution, Point position, Point direction)
        => !puzzle.PositionHasWall(position, direction.GetWall())
        && !solution.Contains(puzzle.NormalizePosition(position + direction));

    private static bool CreatesImpossibleState(this Puzzle puzzle, Solution solution, int colour, Move move)
        =>
            puzzle.CreatesUTurn(solution, colour, move)
            ||
            puzzle.CreatesDeadEnd(solution, move);

    private static bool IsSolved(this Puzzle puzzle, Solution solution, IEnumerable<ColourState> colours)
        =>  colours.All(c => c.Complete) && puzzle.IsFull(solution);

    public static Solution? Solve(this Puzzle puzzle, CancellationToken token)
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
        var previousSolutions = new List<Solution>
        {
            initialSolution
        };
        queue.Push((initialSolution, initialColours, []));
        while (queue.Count != 0 && !token.IsCancellationRequested)
        {
            var (currentSolution, currentColours, currentRejects) = queue.Pop();
            if (puzzle.IsSolved(currentSolution, currentColours))
                return currentSolution;

            var possibleMoves = puzzle.GetPossibleMoves(currentSolution, currentColours);
#if STEP
                puzzle.Print(currentSolution);
                foreach(var (colour, moves) in possibleMoves)
                    Display.Print($"{colour}: {string.Join(", ", moves.Select(m => m.ToString()))}");
                Display.Key();
#endif
            if (possibleMoves.Any(move => !move.Moves.Any() && !currentColours[move.Colour].Complete))
                continue;

            var singleMoves = possibleMoves.Where(p => p.Moves.Length == 1)
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

            if (mandatoryMovesMade)
            {
                queue.Push((currentSolution, currentColours, currentRejects));
                continue;
            }

            foreach (var (colour, moves) in possibleMoves)
                foreach (var move in moves)
                {
                    if (currentRejects.Contains((colour, move.Next)))
                        continue;
                    var newSolution = currentSolution.CloneSolution();
                    var newColours = currentColours.Clone();
                    var newRejects = currentRejects.Select(r => r).ToList();
                    puzzle.MakeMove(newSolution, newColours, colour, move);
                    if (puzzle.CreatesImpossibleState(newSolution, colour, move))
                    {
                        newRejects.Add((colour, move.Next));
                        continue;
                    }
                    if (!previousSolutions.Any(s => Solution.AreEquals(s, newSolution)))
#if DEBUG
                    else
                        Display.Print("solution already tried");
#endif
#if STEP
                    puzzle.Print(newSolution, move.Next);
                    if (Display.Key() == ConsoleKey.Escape)
                        throw new Exception("Step interrupted!");
#endif
                }
        }
        token.ThrowIfCancellationRequested();
        throw new Exception("Solution not found!");
    }
}