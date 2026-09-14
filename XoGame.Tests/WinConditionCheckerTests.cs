using XoGame.Core.Models;
using Xunit;

namespace XoGame.Tests;

public class WinConditionCheckerTests
{
    private readonly WinConditionChecker _rules = new();

    [Theory]
    [InlineData(0, 0, 0, 1, 0, 2)] // top row
    [InlineData(1, 0, 1, 1, 1, 2)] // middle row
    [InlineData(2, 0, 2, 1, 2, 2)] // bottom row
    [InlineData(0, 0, 1, 0, 2, 0)] // left column
    [InlineData(0, 1, 1, 1, 2, 1)] // middle column
    [InlineData(0, 2, 1, 2, 2, 2)] // right column
    public void FindWin_DetectsAcrossLines(int a1, int a2, int b1, int b2, int c1, int c2)
    {
        var board = new Board();
        board.TryPlace(Symbol.X, new Position(a1, a2));
        board.TryPlace(Symbol.X, new Position(b1, b2));
        board.TryPlace(Symbol.X, new Position(c1, c2));

        var win = _rules.FindWin(board);

        Assert.NotNull(win);
        Assert.Equal(Symbol.X, win!.Symbol);
    }

    [Fact]
    public void FindWin_DetectsMainDiagonal()
    {
        var board = new Board();
        board.TryPlace(Symbol.X, new Position(0, 0));
        board.TryPlace(Symbol.X, new Position(1, 1));
        board.TryPlace(Symbol.X, new Position(2, 2));
        Assert.Equal(Symbol.X, _rules.FindWin(board)!.Symbol);
    }

    [Fact]
    public void FindWin_DetectsAntiDiagonal()
    {
        var board = new Board();
        board.TryPlace(Symbol.O, new Position(0, 2));
        board.TryPlace(Symbol.O, new Position(1, 1));
        board.TryPlace(Symbol.O, new Position(2, 0));
        Assert.Equal(Symbol.O, _rules.FindWin(board)!.Symbol);
    }

    [Fact]
    public void FindWin_ReturnsNullBeforeThreeInARow()
    {
        var board = new Board();
        board.TryPlace(Symbol.X, new Position(0, 0));
        board.TryPlace(Symbol.X, new Position(0, 1));
        Assert.Null(_rules.FindWin(board));
    }

    [Fact]
    public void FindWin_ReturnsNullForScatteredMarks()
    {
        var board = new Board();
        board.TryPlace(Symbol.X, new Position(0, 0));
        board.TryPlace(Symbol.O, new Position(1, 1));
        board.TryPlace(Symbol.X, new Position(2, 0));
        Assert.Null(_rules.FindWin(board));
    }

    [Fact]
    public void IsDraw_FalseOnEmptyBoard()
    {
        Assert.False(_rules.IsDraw(new Board()));
    }

    [Fact]
    public void FindWin_NoWinnerOnFullDrawBoard()
    {
        // Final board (no three-in-a-row):
        //   X O X
        //   X O O
        //   O X X
        var moves = new (Symbol, Position)[]
        {
            (Symbol.X, new Position(0, 0)), (Symbol.O, new Position(0, 1)),
            (Symbol.X, new Position(0, 2)), (Symbol.O, new Position(1, 1)),
            (Symbol.X, new Position(1, 0)), (Symbol.O, new Position(1, 2)),
            (Symbol.X, new Position(2, 2)), (Symbol.O, new Position(2, 0)),
            (Symbol.X, new Position(2, 1)),
        };

        var board = new Board();
        foreach (var (symbol, position) in moves)
            board.TryPlace(symbol, position);

        Assert.Null(_rules.FindWin(board));
        Assert.Equal(Symbol.Empty, _rules.FindWin(board)?.Symbol ?? Symbol.Empty);
    }

    [Fact]
    public void IsDraw_TrueWhenFullWithNoWinner()
    {
        // X O X
        // X O O
        // O X X
        var board = new Board();
        var moves = new[]
        {
            (Symbol.X, 0, 0), (Symbol.O, 0, 1), (Symbol.X, 0, 2),
            (Symbol.O, 1, 1), (Symbol.X, 1, 0), (Symbol.O, 1, 2),
            (Symbol.X, 2, 2), (Symbol.O, 2, 0), (Symbol.X, 2, 1),
        };
        foreach (var (symbol, row, col) in moves)
            board.TryPlace(symbol, new Position(row, col));

        Assert.True(board.IsFull());
        Assert.True(_rules.IsDraw(board));
        Assert.Null(_rules.FindWin(board));
    }
}