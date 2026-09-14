namespace XoGame.Core.Models;

/// <summary>
/// The authoritative game state machine. Applies moves, tracks whose turn it is,
/// updates the status after every move, and notifies listeners through events.
/// </summary>
public sealed class Game
{
    private readonly WinConditionChecker _rules;
    private readonly List<Move> _history = [];
    private Board _board;

    public event EventHandler<Position>? CellChanged;
    public event EventHandler<GameStatus>? StatusChanged;
    public event EventHandler? TurnChanged;

    public Game(WinConditionChecker? rules = null)
    {
        _rules = rules ?? WinConditionChecker.Instance;
        _board = new Board();
    }

    public Board Board => _board;

    public Symbol CurrentTurn { get; private set; } = Symbol.X;

    public GameStatus Status { get; private set; } = GameStatus.InProgress;

    /// <summary>Every accepted move, in order.</summary>
    public IReadOnlyList<Move> MoveHistory => _history;

    public Symbol this[Position position] => _board[position].Symbol;

    /// <summary>
    /// The single entry point for placing a mark. Validates the full move (state, symbol, cell),
    /// applies it, detects wins/draws, and advances the turn. Returns false for illegal moves.
    /// </summary>
    public bool TryPlay(Move move)
    {
        if (Status != GameStatus.InProgress)
            return false;
        if (move.Symbol != CurrentTurn)
            return false;
        if (!_board.TryPlace(move.Symbol, move.Position))
            return false;

        _history.Add(move);
        CellChanged?.Invoke(this, move.Position);

        if (_rules.FindWin(_board) is { } win)
        {
            Status = win.Symbol == Symbol.X ? GameStatus.XWins : GameStatus.OWins;
            StatusChanged?.Invoke(this, Status);
            return true;
        }

        if (_rules.IsDraw(_board))
        {
            Status = GameStatus.Draw;
            StatusChanged?.Invoke(this, Status);
            return true;
        }

        CurrentTurn = CurrentTurn == Symbol.X ? Symbol.O : Symbol.X;
        TurnChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Starts a fresh game; the board is rebuilt and history is cleared.</summary>
    public void Reset()
    {
        _board = new Board();
        _history.Clear();
        CurrentTurn = Symbol.X;
        Status = GameStatus.InProgress;
        StatusChanged?.Invoke(this, Status);
        TurnChanged?.Invoke(this, EventArgs.Empty);
    }
}