using XoGame.Core.Models;
using XoGame.Core.Transport;

namespace XoGame.Core.Play;

/// <summary>
/// The opponent, who lives on the other device. Its moves are read from the transport,
/// so it only ever gets the <see cref="Models.Move"/> the peer has already committed to.
/// A Reset command arriving mid-wait is surfaced to the session via <see cref="ResetReceived"/>.
/// </summary>
public sealed class RemotePlayer : Player
{
    private readonly ITransport _transport;

    public RemotePlayer(Symbol symbol, ITransport transport)
        : base(symbol)
    {
        _transport = transport;
    }

    /// <summary>Raised when the peer asks to restart the match while we wait for a move.</summary>
    public event EventHandler? ResetReceived;

    public override async ValueTask<Move?> GetMoveAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var command = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            if (command is null)
                throw new ConnectionClosedException();

            switch (command.Type)
            {
                case CommandType.Move when command.Move is { } move && move.Symbol == Symbol:
                    return move;

                case CommandType.Reset:
                    ResetReceived?.Invoke(this, EventArgs.Empty);
                    return null; // tell the session to re-evaluate whose turn it is now
            }
        }
    }
}