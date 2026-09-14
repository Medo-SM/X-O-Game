using XoGame.Core.Models;
using Xunit;

namespace XoGame.Tests;

public class BoardTests
{
    [Fact]
    public void Cells_SixteenPositionsRowMajor()
    {
        var board = new Board();
        var rows = new[] { (0, 0), (0, 1), (0, 2), (1, 0), (1, 1), (1, 2), (2, 0), (2, 1), (2, 2) };
        Assert.Equal(rows.Length, board.Cells.Count);

        for (var i = 0; i < rows.Length; i++)
        {
            var (row, col) = rows[i];
            var cell = board.Cells[i];
            Assert.Equal(new Position(row, col), cell.Position);
            Assert.Equal(i, cell.Position.Index);
        }
    }

    [Fact]
    public void TryPlace_AcceptsValidEmptyPosition()
    {
        var board = new Board();
        Assert.True(board.TryPlace(Symbol.X, new Position(1, 1)));
        Assert.Equal(Symbol.X, board[1, 1].Symbol);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    public void TryPlace_RejectsOutOfBounds(int row, int col)
    {
        var board = new Board();
        Assert.False(board.TryPlace(Symbol.X, new Position(row, col)));
    }

    [Fact]
    public void TryPlace_RejectsOccupiedCell()
    {
        var board = new Board();
        board.TryPlace(Symbol.X, new Position(0, 0));
        Assert.False(board.TryPlace(Symbol.O, new Position(0, 0)));
        Assert.Equal(Symbol.X, board[0, 0].Symbol);
    }

    [Fact]
    public void TryPlace_RejectsEmptySymbol()
    {
        var board = new Board();
        Assert.False(board.TryPlace(Symbol.Empty, new Position(0, 0)));
    }

    [Fact]
    public void IsFull_OnlyWhenNoFreeCells()
    {
        var board = new Board();
        Assert.False(board.IsFull());
        for (var i = 0; i < Position.CellCount; i++)
            board.TryPlace(i % 2 == 0 ? Symbol.X : Symbol.O, Position.FromIndex(i));
        Assert.True(board.IsFull());
    }
}