using XoGame.Core.Models;

namespace XoGame.Core.Play;

/// <summary>
/// Base class of every participant in a match. It is the Strategy abstraction for
/// "where does the next move come from": the <see cref="LocalPlayer"/> takes it from
/// the human on this device, the <see cref="RemotePlayer"/> from the transport.
/// </summary>
public abstract class Player
{
    protected Player(Symbol symbol)
    {
        Symbol = symbol;
    }

    /// <summary>The mark this player places on the board.</summary>
    public Symbol Symbol { get; }

    /// <summary>
    /// Produces this player's next move, waiting for it as long as needed.
    /// A <see langword="null"/> result means the session should re-evaluate the turn
    /// (a reset arrived from the peer while this player was waiting).
    /// </summary>
    public abstract ValueTask<Move?> GetMoveAsync(CancellationToken cancellationToken = default);

    public override string ToString() => $"Player {Symbol}";
}