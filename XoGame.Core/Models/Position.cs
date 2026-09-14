namespace XoGame.Core.Models;

/// <summary>An immutable location on the board (values 0..2 for both axes).</summary>
public readonly record struct Position(int Row, int Col)
{
    public const int BoardSize = 3;
    public const int CellCount = BoardSize * BoardSize;

    /// <summary>0..8 index in row-major order, handy for UI grids and codecs.</summary>
    public readonly int Index => Row * BoardSize + Col;

    public static Position FromIndex(int index) => new(index / BoardSize, index % BoardSize);

    public bool IsValid => (uint)Row < BoardSize && (uint)Col < BoardSize;

    public override readonly string ToString() => $"{Row},{Col}";
}