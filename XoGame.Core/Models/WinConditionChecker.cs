namespace XoGame.Core.Models;

/// <summary>
/// The rules engine. Holding this in its own class (instead of inside <see cref="Game"/>)
/// separates rule knowledge from state management and makes both easy to test in isolation.
/// </summary>
public sealed class WinConditionChecker
{
    /// <summary>All eight winning lines: 3 rows, 3 columns, and 2 diagonals.</summary>
    private static readonly Position[][] Lines = BuildLines();

    public static WinConditionChecker Instance { get; } = new();

    private static Position[][] BuildLines()
    {
        var lines = new List<Position[]>(8);
        for (var i = 0; i < Position.BoardSize; i++)
        {
            lines.Add([new Position(i, 0), new Position(i, 1), new Position(i, 2)]);
            lines.Add([new Position(0, i), new Position(1, i), new Position(2, i)]);
        }

        lines.Add([new Position(0, 0), new Position(1, 1), new Position(2, 2)]);
        lines.Add([new Position(0, 2), new Position(1, 1), new Position(2, 0)]);
        return lines.ToArray();
    }

    /// <summary>Returns the winning line and its symbol, or null when nobody has won.</summary>
    public WinLine? FindWin(Board board)
    {
        foreach (var line in Lines)
        {
            var first = board[line[0]].Symbol;
            if (first == Symbol.Empty)
                continue;

            var complete = true;
            for (var i = 1; i < line.Length; i++)
            {
                if (board[line[i]].Symbol != first)
                {
                    complete = false;
                    break;
                }
            }

            if (complete)
                return new WinLine(first, line);
        }

        return null;
    }

    /// <summary>The board has no free cells left.</summary>
    public bool IsDraw(Board board) => board.IsFull();
}