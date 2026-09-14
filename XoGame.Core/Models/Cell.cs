namespace XoGame.Core.Models;

/// <summary>One square of the board. Symbol is only mutable by the owning Board.</summary>
public sealed class Cell
{
    internal Cell(Position position)
    {
        Position = position;
    }

    public Position Position { get; }

    public Symbol Symbol { get; internal set; } = Symbol.Empty;
}