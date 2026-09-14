using XoGame.Core.Models;

namespace XoGame.ConsoleApp;

/// <summary>
/// Draws the board into a reserved area of the terminal. All writes go through one lock so
/// that event-driven redraws (which happen on background threads during a Bluetooth match)
/// can never interleave with the player prompts printed by the main thread.
/// </summary>
public sealed class GameRenderer
{
    private const int Width = 60;

    private readonly object _sync = new();
    private readonly string _title;

    public GameRenderer(string title)
    {
        _title = title;
        Console.Clear();
        Console.CursorVisible = false;
    }

    /// <summary>Repaints the title line, the status line and the 3x3 board in place.</summary>
    public void Render(Game game, string statusText)
    {
        lock (_sync)
        {
            WriteAt(0, _title);
            WriteAt(1, statusText);
            WriteAt(2, BuildRow(game, 0));
            WriteAt(3, "    -      -     ");
            WriteAt(4, BuildRow(game, 1));
            WriteAt(5, "    -      -     ");
            WriteAt(6, BuildRow(game, 2));
            WriteAt(7, "Moves: " + game.MoveHistory.Count);
        }
    }

    /// <summary>Prints a one-off message below the board, then clears the hint row.</summary>
    public void Message(string text)
    {
        lock (_sync)
        {
            WriteAt(9, text);
        }
    }

    /// <summary>Prompts the human for input under the board; stays serialized with renders.</summary>
    public string ReadInput(string prompt)
    {
        lock (_sync)
        {
            WriteAt(9, prompt);
            Console.CursorLeft = 0;
            Console.CursorTop = 10;
            return Console.ReadLine() ?? string.Empty;
        }
    }

    public void Cleanup()
    {
        Console.CursorVisible = true;
        Console.Clear();
    }

    private static string BuildRow(Game game, int row)
    {
        string Cell(Cell cell) => cell.Symbol switch
        {
            Symbol.Empty => (cell.Position.Index + 1).ToString(),
            Symbol.X => "X",
            _ => "O",
        };

        return $"  {Cell(game.Board[row, 0])}  |  {Cell(game.Board[row, 1])}  |  {Cell(game.Board[row, 2])}  ";
    }

    private static void WriteAt(int row, string text)
    {
        Console.SetCursorPosition(0, row);
        var padded = text.Length >= Width ? text : text.PadRight(Width);
        Console.Write(padded);
    }
}