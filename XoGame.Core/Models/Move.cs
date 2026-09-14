namespace XoGame.Core.Models;

/// <summary>A single placement of a symbol on the board.</summary>
public sealed record Move(Symbol Symbol, Position Position)
{
    public bool IsValid => Symbol is Symbol.X or Symbol.O && Position.IsValid;
}