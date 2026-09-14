using XoGame.Core.Models;

namespace XoGame.Core.Transport;

/// <summary>
/// A message exchanged between the two devices/sides.
/// A <see cref="Models.Move"/> carries a placement; the Reset command re-starts the match.
/// </summary>
public enum CommandType : byte
{
    Move = 1,
    Reset = 2,
}