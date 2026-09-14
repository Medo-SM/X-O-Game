namespace XoGame.Core.Models;

/// <summary>
/// The 3x3 game grid. Owns the <see cref="Cell"/> objects and is the only type that
/// mutates their symbols, enforcing the two core validation rules:
/// a position must be in bounds, and it must be empty.
/// </summary>
public sealed class Board
{
    private readonly Cell[,] _cells;
    private readonly List<Cell> _cellsFlat;

    public Board()
    {
        _cells = new Cell[Position.BoardSize, Position.BoardSize];
        _cellsFlat = new List<Cell>(Position.CellCount);
        for (var row = 0; row < Position.BoardSize; row++)
        {
            for (var col = 0; col < Position.BoardSize; col++)
            {
                var cell = new Cell(new Position(row, col));
                _cells[row, col] = cell;
                _cellsFlat.Add(cell);
            }
        }
    }

    /// <summary>All nine cells, row-major.</summary>
    public IReadOnlyList<Cell> Cells => _cellsFlat;

    public Cell this[Position position] => _cells[position.Row, position.Col];

    public Cell this[int row, int col] => _cells[row, col];

    public bool IsInBounds(Position position) => position.IsValid;

    public bool IsEmpty(Position position) => IsInBounds(position) && this[position].Symbol == Symbol.Empty;

    public bool IsFull() => _cellsFlat.All(cell => cell.Symbol != Symbol.Empty);

    /// <summary>Attempts to place a symbol. Returns false when the move is illegal.</summary>
    public bool TryPlace(Symbol symbol, Position position)
    {
        if (symbol is Symbol.Empty)
            return false;
        if (!IsEmpty(position))
            return false;

        this[position].Symbol = symbol;
        return true;
    }
}