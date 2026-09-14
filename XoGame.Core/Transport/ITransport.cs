namespace XoGame.Core.Transport;

/// <summary>
/// The seam that separates "how a move crosses the wire" from the game itself.
/// Swapping implementations turns the exact same game session into a hotseat game,
/// a Bluetooth match, or a unit test of two virtual opponents.
/// </summary>
public interface ITransport : IAsyncDisposable
{
    /// <summary>Pushes a command to the peer. Returns null when the channel was closed.</summary>
    ValueTask SendAsync(Command command, CancellationToken cancellationToken = default);

    /// <summary>Waits for the next command from the peer, or null when they disconnected.</summary>
    ValueTask<Command?> ReceiveAsync(CancellationToken cancellationToken = default);
}