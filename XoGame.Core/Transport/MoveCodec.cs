using XoGame.Core.Models;
using XoGame.Core.Transport;

namespace XoGame.Core.Transport;

/// <summary>
/// Serializes and deserializes <see cref="Command"/>s to/from a compact byte format:
/// move:  [type][symbol][row][col]   reset:  [type]
/// Keeping this standalone means any transport (in-memory, Bluetooth socket) shares one protocol.
/// </summary>
public static class MoveCodec
{
    public static byte[] Encode(Command command)
    {
        if (command.Type == CommandType.Reset)
            return [(byte)CommandType.Reset];

        var move = command.Move!;
        return [(byte)CommandType.Move, (byte)move.Symbol, (byte)move.Position.Row, (byte)move.Position.Col];
    }

    /// <summary>Returns null when the payload is a malformed command.</summary>
    public static Command? Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return null;

        switch ((CommandType)data[0])
        {
            case CommandType.Reset when data.Length == 1:
                return Command.Reset;

            case CommandType.Move when data.Length == 4:
                var symbol = (Symbol)data[1];
                var position = new Position(data[2], data[3]);
                if (symbol is not (Symbol.X or Symbol.O))
                    return null;
                if (!position.IsValid)
                    return null;
                return Command.ForMove(new Move(symbol, position));

            default:
                return null;
        }
    }
}