using XoGame.Core.Models;
using XoGame.Core.Transport;
using Xunit;

namespace XoGame.Tests;

public class MoveCodecTests
{
    [Theory]
    [InlineData(Symbol.X, 0, 0)]
    [InlineData(Symbol.O, 1, 2)]
    [InlineData(Symbol.X, 2, 1)]
    [InlineData(Symbol.O, 2, 2)]
    public void Move_RoundTrips(Symbol symbol, int row, int col)
    {
        var command = Command.ForMove(new Move(symbol, new Position(row, col)));
        var encoded = MoveCodec.Encode(command);

        Assert.Equal(4, encoded.Length);

        var decoded = MoveCodec.Decode(encoded);
        Assert.Equal(command, decoded);
    }

    [Fact]
    public void Reset_RoundTrips()
    {
        var encoded = MoveCodec.Encode(Command.Reset);
        Assert.Single(encoded);
        Assert.Equal(Command.Reset, MoveCodec.Decode(encoded));
    }

    [Theory]
    [InlineData((byte)0xff, 0, 0)]
    [InlineData((byte)CommandType.Move, 9, 9)]  // out of bounds
    [InlineData((byte)CommandType.Move, 0, 0)]  // missing symbol/coords
    public void Decode_RejectsMalformed(byte type, int row, int col)
    {
        var payload = new[] { type, (byte)Symbol.Empty, (byte)row, (byte)col };
        Assert.Null(MoveCodec.Decode(payload));
    }

    [Fact]
    public void Decode_RejectsEmptyBuffer() => Assert.Null(MoveCodec.Decode(ReadOnlySpan<byte>.Empty));
}