namespace XoGame.Core.Transport;

/// <summary>Raised when the peer goes away while a session is still waiting for it.</summary>
public sealed class ConnectionClosedException : Exception
{
    public ConnectionClosedException(string? message = null, Exception? inner = null)
        : base(message ?? "The connection to the opponent was closed.", inner)
    {
    }
}