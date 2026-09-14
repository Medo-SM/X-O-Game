using System.Windows.Forms;
using XoGame.Core.Models;
using XoGame.Core.Play;

namespace XoGame.App;

/// <summary>
/// Connects the WinForms window to the game engine: it runs the <see cref="GameSession"/>,
/// feeds cell taps into it and pushes every engine change back to the UI thread.
/// </summary>
internal sealed class GameController : IDisposable
{
    private readonly GameForm _form;
    private readonly GameSession _session;
    private readonly object _gate = new();
    private volatile bool _disconnected;
    private volatile bool _disposed;

    public GameController(GameForm form, GameSession session)
    {
        _form = form;
        _session = session;

        _session.Game.CellChanged += OnCellChanged;
        _session.Game.TurnChanged += OnTurnChanged;
        _session.Game.StatusChanged += OnStatusChanged;
        _session.Disconnected += OnDisconnected;

        _ = _session.RunAsync();
        Refresh(this, EventArgs.Empty);
    }

    /// <summary>Plays the square the player tapped, but only when it is this side's turn.</summary>
    public void SubmitCell(int index)
    {
        if (_disconnected || !_session.CanPlayLocally)
            return;

        _session.SubmitMove(Position.FromIndex(index));
    }

    /// <summary>Restarts the match (and asks the peer to restart too in Bluetooth games).</summary>
    public void ResetGame() => _ = _session.ResetAsync();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _session.Game.CellChanged -= OnCellChanged;
        _session.Game.TurnChanged -= OnTurnChanged;
        _session.Game.StatusChanged -= OnStatusChanged;
        _session.Disconnected -= OnDisconnected;

        _ = _session.DisposeAsync();
    }

    private void OnCellChanged(object? sender, Position position) => Refresh(this, EventArgs.Empty);

    private void OnTurnChanged(object? sender, EventArgs e) => Refresh(this, EventArgs.Empty);

    private void OnStatusChanged(object? sender, GameStatus status) => Refresh(this, EventArgs.Empty);

    private void OnDisconnected(object? sender, EventArgs e)
    {
        lock (_gate)
        {
            if (_disconnected)
                return;
            _disconnected = true;
        }
        Refresh(sender, e);
    }

    private void Refresh(object? sender, EventArgs e)
    {
        if (_disposed)
            return;

        var game = _session.Game;
        var text = new string[9];
        var enabled = !_disconnected && _session.CanPlayLocally;

        for (var i = 0; i < 9; i++)
            text[i] = game.Board[i / 3, i % 3].Symbol switch
            {
                Symbol.Empty => (i + 1).ToString(),
                Symbol.X => "X",
                _ => "O",
            };

        var status = game.Status switch
        {
            GameStatus.InProgress => $"It's {game.CurrentTurn}'s turn.",
            GameStatus.XWins => "X wins!",
            GameStatus.OWins => "O wins!",
            _ => "It's a draw!",
        };

        if (_disconnected)
            status = "Opponent disconnected.";

        if (_form.IsDisposed || !_form.IsHandleCreated)
            return;

        _form.BeginInvoke((MethodInvoker)(() => _form.ApplyState(text, enabled, status)));
    }
}