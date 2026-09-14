using XoGame.Core.Models;
using XoGame.Core.Transport;

namespace XoGame.Core.Play;

/// <summary>
/// Orchestrates one match: it asks the player whose turn it is for a move, applies it
/// to the <see cref="Game"/>, and relays it to the peer when needed.
///
/// It runs exactly the same in every mode:
///   hotseat        -> two <see cref="LocalPlayer"/>s, no transport
///   Bluetooth host -> a <see cref="LocalPlayer"/> (X) plus a <see cref="RemotePlayer"/> (O)
///   Bluetooth client-> a <see cref="RemotePlayer"/> (X) plus a <see cref="LocalPlayer"/> (O)
/// </summary>
public sealed class GameSession : IAsyncDisposable
{
    private readonly Game _game;
    private readonly Player _playerX;
    private readonly Player _playerO;
    private readonly ITransport? _transport;
    private readonly bool _isRemote;
    private readonly Symbol? _localSymbol;
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _disposed;
    private Task? _loopTask;

    /// <summary>Raised when the peer disconnects mid-session.</summary>
    public event EventHandler? Disconnected;

    public GameSession(Player playerX, Player playerO, ITransport? transport = null, Symbol? localSymbol = null)
    {
        _game = new Game();
        _playerX = playerX;
        _playerO = playerO;
        _transport = transport;
        _localSymbol = localSymbol;
        _isRemote = transport is not null;

        if (_playerX is RemotePlayer remoteX)
            remoteX.ResetReceived += OnRemoteReset;
        if (_playerO is RemotePlayer remoteO)
            remoteO.ResetReceived += OnRemoteReset;
    }

    public Game Game => _game;

    public bool IsRemoteMode => _isRemote;

    /// <summary>The mark this device places, or null in hotseat mode where both do.</summary>
    public Symbol? LocalSymbol => _localSymbol;

    /// <summary>True when the human (or hotseat player) may currently claim a square.</summary>
    public bool CanPlayLocally => _game.Status == GameStatus.InProgress && (_localSymbol is null || _game.CurrentTurn == _localSymbol);

    public Symbol LocalPlayerSymbolOrCurrent => _localSymbol ?? _game.CurrentTurn;

    /// <summary>Starts the play loop and runs until the session is disposed or cancelled.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        _loopTask ??= PlayLoopAsync(linked.Token);
        await _loopTask.ConfigureAwait(false);
    }

    /// <summary>Feeds a human's tap into whichever local player is on turn.</summary>
    public void SubmitMove(Position position)
    {
        var current = CurrentPlayer;
        if (current is LocalPlayer local)
            local.SubmitMove(position);
    }

    /// <summary>Restarts the match locally and, in remote mode, asks the peer to do the same.</summary>
    public async ValueTask ResetAsync(CancellationToken cancellationToken = default)
    {
        _game.Reset();
        if (_isRemote && _transport is not null)
            await _transport.SendAsync(Command.Reset, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>A Reset the peer sent: restart locally, don't echo it back.</summary>
    private void OnRemoteReset(object? sender, EventArgs e) => _game.Reset();

    private Player CurrentPlayer => _game.CurrentTurn == Symbol.X ? _playerX : _playerO;

    private async Task PlayLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var current = CurrentActor;

                var move = await current.GetMoveAsync(cancellationToken).ConfigureAwait(false);

                // A null move simply means the turn has changed (peer asked for a reset).
                if (move is null)
                    continue;

                // Ignore moves that no longer fit the board state (e.g. stale messages after a
                // reset or after the game already ended) - both sides re-synchronize from Game.
                if (_game.Status != GameStatus.InProgress)
                    continue;
                if (!_game.TryPlay(move))
                    continue;

                if (_isRemote && current is LocalPlayer && _transport is not null)
                    await _transport.SendAsync(Command.ForMove(move), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (ConnectionClosedException)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// Whose move is the session waiting for right now. After a finished game the only
    /// way forward is a message from the peer, so keep the remote side's transport read
    /// alive instead of idling forever on a local player.
    /// </summary>
    private Player CurrentActor
        => _isRemote && _game.Status != GameStatus.InProgress
            ? (_playerX is RemotePlayer ? _playerX : _playerO)
            : CurrentPlayer;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _cts.Cancel();
            if (_transport is not null)
                await _transport.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _cts.Dispose();
        }
    }
}