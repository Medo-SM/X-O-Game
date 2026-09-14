using XoGame.Core.Models;

namespace XoGame.Core.Transport;

/// <summary>A single packet sent between the host and the client.</summary>
public sealed record Command(CommandType Type, Move? Move = null)
{
    public static Command ForMove(Move move) => new(CommandType.Move, move);

    public static Command Reset { get; } = new(CommandType.Reset);
}