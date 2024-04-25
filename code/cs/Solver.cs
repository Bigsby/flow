using System.Numerics;
using ISolution = System.Collections.Generic.IDictionary<System.Numerics.Complex, int>;
using Solution = System.Collections.Generic.Dictionary<System.Numerics.Complex, int>;

internal static class Solver
{
    const bool DEBUG = true;
    const bool STEP = true;

    record struct ColourState(int Colour, bool Complete, Complex Head, Complex End);

    static (Complex, Walls)[] DIRECTIONS = [
        (new Complex(-1, 0), Walls.LEFT),
        (new Complex(0, -1), Walls.UP),
        (new Complex(1, 0), Walls.RIGHT),
        (new Complex(0, 1), Walls.DOWN),
    ];

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
    
    private static IEnumerable<(Complex, Complex)> GetPossibleMoves(this Puzzle puzzle, ISolution solution, int colour, Complex head, Complex end)
    {
            var result = new List<(Complex, Complex)>();
            foreach (var neighbour in puzzle.GetNeighbours(head))
            {
                Debug($"{Display.GetColourDot(colour)} {head} > {neighbour}");
                if (solution.ContainsKey(neighbour))
                    continue;
                if (!puzzle.HasSameColourNeighbour(solution, neighbour, head, end, colour))
                    result.Add((neighbour, head));
                else
                    Debug($"Has same colour {Display.GetColourDot(colour)} {head} {neighbour} {end}");
            }
            return result;
    }

    private static (int, IEnumerable<(Complex, Complex)>)[] GetPossibleMoves(this Puzzle puzzle, ISolution solution, IEnumerable<ColourState> colours)
    {
        var result = new List<(int, IEnumerable<(Complex, Complex)>)>();
        foreach (var (colour, complete, head, end) in colours)
        {
            if (complete)
                continue;
            result.Add((colour, puzzle.GetPossibleMoves(solution, colour, head, end)));
            result.Add((colour, puzzle.GetPossibleMoves(solution, colour, end, head)));
        }
        return result.ToArray();
    }

    private static ISolution Clone(this ISolution solution)
        => solution.ToDictionary(pair => pair.Key, pair => pair.Value);

    private static IList<ColourState> Clone(this IList<ColourState> list)
        => list.Select(c => c with { }).ToList();

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

            var mustMoves = possibleMoves.Where(p => p.Item2.Count() == 1).ToArray();
            foreach (var (colour, moves) in mustMoves)
            {
                var (next, previous) = moves.First();
                currentSolution[next] = colour;
                var colourState = currentColours[colour];
                var otherEnd = previous == colourState.Head ? colourState.End : colourState.Head;
                currentColours[colour] = colourState with {
                    Complete = next == otherEnd || puzzle.GetNeighbours(next).Any(n => n == otherEnd),
                    Head = next,
                    End = otherEnd
                }; //(colour, puzzle.GetNeighbours(next).Any(n => n == otherEnd), next, otherEnd);
            }
            if (mustMoves.Any())
            {
                queue.Enqueue((currentSolution, currentColours));
                continue;
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