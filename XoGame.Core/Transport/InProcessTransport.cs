using System.Threading.Channels;
using XoGame.Core.Transport;

namespace XoGame.Core.Transport;

/// <summary>
/// A transport that communicates entirely in memory. It powers the hotseat mode
/// (no Bluetooth involved) and lets tests simulate a two-device Bluetooth match
/// without any hardware.
/// </summary>
public sealed class InProcessTransport : ITransport
{
    private readonly Channel<Command> _channel = Channel.CreateUnbounded<Command>();

    public ValueTask SendAsync(Command command, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(command, cancellationToken);

    public async ValueTask<Command?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <summary>Simulates the peer terminating the connection.</summary>
    public void Close() => _channel.Writer.TryComplete();

    public ValueTask DisposeAsync()
    {
        Close();
        return ValueTask.CompletedTask;
    }
}