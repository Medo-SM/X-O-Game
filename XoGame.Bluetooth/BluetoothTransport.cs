using XoGame.Core.Transport;

namespace XoGame.Bluetooth;

/// <summary>
/// An <see cref="ITransport"/> speaking the shared MoveCodec framing over a Bluetooth
/// RFCOMM stream. Frames are length-prefixed (4-byte little-endian) so the exact same
/// protocol used by the in-process transport also works across devices.
/// </summary>
public sealed class BluetoothTransport : ITransport
{
    private const int MaxFrameLength = 1024;

    private readonly Stream _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly byte[] _headerBuffer = new byte[4];

    internal BluetoothTransport(Stream stream)
    {
        _stream = stream;
    }

    /// <summary>The stream produced by the peer end of a connected Bluetooth client.</summary>
    public Stream Stream => _stream;

    public async ValueTask SendAsync(Command command, CancellationToken cancellationToken = default)
    {
        var payload = MoveCodec.Encode(command);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(_headerBuffer, payload.Length);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(_headerBuffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            await _stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            throw new ConnectionClosedException("Writing to the Bluetooth peer failed.", ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask<Command?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _stream.ReadExactlyAsync(_headerBuffer, cancellationToken).ConfigureAwait(false);

            var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(_headerBuffer);
            if (length is <= 0 or > MaxFrameLength)
                return null;

            var payload = new byte[length];
            await _stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
            return MoveCodec.Decode(payload);
        }
        catch (Exception ex) when (ex is EndOfStreamException or IOException or ObjectDisposedException)
        {
            return null; // peer is gone
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public ValueTask DisposeAsync()
    {
        _writeLock.Dispose();
        return _stream.DisposeAsync();
    }
}