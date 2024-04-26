using System.Numerics;
using ISolution = System.Collections.Generic.IDictionary<System.Numerics.Complex, int>;
using Solution = System.Collections.Generic.Dictionary<System.Numerics.Complex, int>;

internal static class Solver
{
    const bool DEBUG = false;
    const bool STEP = true;

    record struct ColourState(int Colour, bool Complete, Complex Head, Complex End);
    record struct Move(Complex Next, Complex Previous);
    record struct ColourMoves(int Colour, IEnumerable<Move> Moves);

    static (Complex, Walls)[] DIRECTIONS = [
        (new Complex(-1, 0), Walls.LEFT),
        (new Complex(0, -1), Walls.UP),
        (new Complex(1, 0), Walls.RIGHT),
        (new Complex(0, 1), Walls.DOWN),
    ];

    private static Walls GetDirectionWall(Complex direciton)
        => DIRECTIONS.First(d => d.Item1 == direciton).Item2;

    public static Walls GetNeighbour(this Puzzle puzzle, Complex position, Complex direction)
    {
        var neighbour = position + direction;
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
            var actualNeighbour = new Complex((neighbour.Real + puzzle.MaxX + 1) % (puzzle.MaxX + 1), (neighbour.Imaginary + puzzle.MaxY + 1) % (puzzle.MaxY + 1));
            if (puzzle.Positions.ContainsKey(actualNeighbour))
                yield return actualNeighbour;
        }
    }

    private static void Debug(string text)
    {
        if (DEBUG)
            Display.Write(text);
    }


    private static bool HasSameColourNeighbour(this Puzzle puzzle, ISolution solution, Complex position, Complex previous, Complex end, int colour)
        => puzzle.GetNeighbours(position).Any(n => n != previous && n != end && solution.ContainsKey(n) && solution[n] == colour);

    private static IEnumerable<Move> GetPossibleMoves(this Puzzle puzzle, ISolution solution, int colour, Complex head, Complex end)
    {
        var result = new List<Move>();
        foreach (var neighbour in puzzle.GetNeighbours(head))
        {
            Debug($"{Display.GetColourDot(colour)} {head} > {neighbour}");
            if (solution.ContainsKey(neighbour))
                continue;
            if (!puzzle.HasSameColourNeighbour(solution, neighbour, head, end, colour))
                result.Add(new(neighbour, head));
            else
                Debug($"Has same colour {Display.GetColourDot(colour)} {head} {neighbour} {end}");
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

    private static Complex GetMoveDirection(Complex next, Complex previous)
    {
        if (next.Real == previous.Real)
            return new Complex(0, next.Imaginary - previous.Imaginary);
        return next.Real - previous.Real;
    }

    private static ColourMoves[] GetCorners(this Puzzle puzzle, ColourMoves[] colourMoves, ISolution solution)
    {
        var result = new List<ColourMoves>();
        foreach (var (colour, moves) in colourMoves)
        {
            foreach (var (next, previous) in moves)
            {
                var direction = GetMoveDirection(next, previous);
                var wall = GetDirectionWall(direction);
                var position = puzzle.Positions[next];
                if (position.HasFlag(wall))
                {
                    switch (wall)
                    {
                        case Walls.UP:
                        case Walls.DOWN:
                            if (position.HasFlag(Walls.LEFT) ^ position.HasFlag(Walls.RIGHT))
                                result.Add(new(colour, new[] { new Move(next, previous) }));
                            break;
                        case Walls.LEFT:
                        case Walls.RIGHT:
                            if (position.HasFlag(Walls.UP) ^ position.HasFlag(Walls.DOWN))
                                result.Add(new(colour, new[] { new Move(next, previous) }));
                            break;
                    }
                }
            }
        }
        Display.Key();
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

    public static IReadOnlyDictionary<Complex, int> Solve(this Puzzle puzzle)
    {
        var initialSolution = new Solution();
        var initialColours = new List<ColourState>();
        foreach (var (start, end, colour) in puzzle.Colours.Select((c, i) => (c.Item1, c.Item2, i)))
        {
            initialSolution[start] = colour;
            initialSolution[end] = colour;
            initialColours.Add(new(colour, false, start, end));
        }
        var queue = new Queue<(ISolution, IList<ColourState>)>();
        queue.Enqueue((initialSolution, initialColours));
        while (queue.Any())
        {
            var (currentSolution, currentColours) = queue.Dequeue();
            if (DEBUG)
            {
                Debug("From >>>>>>>>");
                puzzle.Print(currentSolution);
            }

            var mandatoryMoves = false;

            var possibleMoves = puzzle.GetPossibleMoves(currentSolution, currentColours);
            if (DEBUG)
            {
                foreach (var (colour, complete, head, end) in currentColours)
                    Debug($"{Display.GetColourDot(colour)} {complete} {head} {end}");
                Debug("Possible:");
                foreach (var (colour, moves) in possibleMoves)
                    Debug($" = {Display.GetColourDot(colour)}: {string.Join(", ", moves.Select(m => m.ToString()))}");
                Display.Key();
            }

            var mustMoves = possibleMoves.Where(p => p.Moves.Count() == 1)
                .ToArray();

            foreach (var (colour, moves) in mustMoves)
            {
                mandatoryMoves = true;
                puzzle.MakeMove(currentSolution, currentColours, colour, moves.First());
            }

            puzzle.Print(currentSolution);
            var otherMoves = possibleMoves.Except(mustMoves).ToArray();
            var corners = puzzle.GetCorners(otherMoves, currentSolution);
            if (DEBUG)
            {
                Display.Write("Corners:");
                foreach(var (colour, moves) in corners)
                    Display.Write($"{Display.GetColourDot(colour)}: {moves.First()}");
                Display.Key();
            }

            foreach (var (colour, moves) in corners)
            {
                mandatoryMoves = true;
                puzzle.MakeMove(currentSolution, currentColours, colour, moves.First());
            }

            if (mandatoryMoves)
            {
                if (currentColours.All(c => c.Complete))
                    return currentSolution.AsReadOnly();
                if (DEBUG)
                {
                    Display.Write("After manadory");
                    puzzle.Print(currentSolution);
                    Display.Key();
                }
                queue.Enqueue((currentSolution, currentColours));
                continue;
            } else if (DEBUG)
            {
                Display.Write("No more mandatory");
                Display.Key();
            }


            if (DEBUG)
            {
                Debug("After possible >>>>>>>>>>");
                puzzle.Print(currentSolution, false, true);
                foreach (var (colour, complete, head, end) in currentColours)
                    Debug($"{Display.GetColourDot(colour)} {complete} {head} {end}");
                Display.Key();
            }

            if (currentColours.All(c => c.Complete))
                return currentSolution.AsReadOnly();

            if (DEBUG)
            {
                Debug("NO MORE MUSTS");
                Display.Key();
            }

            foreach (var (colour, complete, head, end) in currentColours)
            {
                if (complete)
                    continue;
                var pathFound = false;
                foreach (var neighbour in puzzle.GetNeighbours(head))
                {
                    if (DEBUG)
                    {
                        Debug($"{Display.GetColourDot(colour)} {head} > {neighbour}");
                        if (STEP)
                            Display.Key();
                    }
                    if ((currentSolution.ContainsKey(neighbour) && neighbour != end)
                        ||
                        puzzle.HasSameColourNeighbour(currentSolution, neighbour, head, end, colour))
                    {
                        Debug(string.Join(", ", currentSolution.Select(pair => $"{pair.Key}:{pair.Value}")));
                        Debug($"{currentSolution.ContainsKey(neighbour) && neighbour != end}");
                        Debug($"{puzzle.HasSameColourNeighbour(currentSolution, neighbour, head, end, colour)}");
                        continue;
                    }
                    Debug("Path found!");

                    pathFound = true;
                    var newSolution = currentSolution.Clone();
                    var newColours = currentColours.Clone();
                    newSolution[neighbour] = colour;
                    newColours[colour] = newColours[colour] with { Complete = neighbour == end };
                    if (newColours.All(c => c.Complete))
                        return newSolution.AsReadOnly();
                    if (DEBUG)
                    {
                        Display.Write($"{Display.GetColourDot(colour)} Into --------------");
                        puzzle.Print(newSolution.AsReadOnly(), false, false);
                    }
                    queue.Enqueue((newSolution, newColours));
                }
                if (STEP)
                    Display.Key();
                if (!pathFound)
                    break;
            }
            if (DEBUG)
                Display.Write("---------- //// ---------");

        }
        throw new Exception("Solution not found");
    }
}