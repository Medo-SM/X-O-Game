using System.Threading.Channels;
using XoGame.Core.Models;

namespace XoGame.Core.Play;

/// <summary>
/// A player controlled by the human at this device. The UI feeds positions into
/// the internal channel and <see cref="GetMoveAsync"/> hands them back as moves.
/// </summary>
public sealed class LocalPlayer : Player
{
    private readonly Channel<Position> _moves = Channel.CreateUnbounded<Position>();

    public LocalPlayer(Symbol symbol)
        : base(symbol)
    {
    }

    /// <summary>Called by the UI (or tests) whenever the human taps an empty square.</summary>
    public void SubmitMove(Position position) => _moves.Writer.TryWrite(position);

    public override async ValueTask<Move?> GetMoveAsync(CancellationToken cancellationToken = default)
    {
        var position = await _moves.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return new Move(Symbol, position);
    }
}