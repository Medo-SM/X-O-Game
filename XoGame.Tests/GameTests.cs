using XoGame.Core.Models;
using Xunit;

namespace XoGame.Tests;

public class GameTests
{
    [Fact]
    public void NewGame_XStarts_AndTurnsAlternate()
    {
        var game = new Game();
        Assert.Equal(Symbol.X, game.CurrentTurn);

        Assert.True(game.TryPlay(new Move(Symbol.X, new Position(0, 0))));
        Assert.Equal(Symbol.O, game.CurrentTurn);

        Assert.True(game.TryPlay(new Move(Symbol.O, new Position(1, 1))));
        Assert.Equal(Symbol.X, game.CurrentTurn);
    }

    [Fact]
    public void TryPlay_RejectsWrongSymbol()
    {
        var game = new Game();
        Assert.False(game.TryPlay(new Move(Symbol.O, new Position(0, 0))));
        Assert.Equal(Symbol.Empty, game[new Position(0, 0)]);
    }

    [Fact]
    public void TryPlay_RejectsOccupiedCell()
    {
        var game = new Game();
        game.TryPlay(new Move(Symbol.X, new Position(0, 0)));
        Assert.False(game.TryPlay(new Move(Symbol.O, new Position(0, 0))));
    }

    [Fact]
    public void TryPlay_RejectsOutOfBounds()
    {
        var game = new Game();
        Assert.False(game.TryPlay(new Move(Symbol.X, new Position(9, 9))));
    }

    [Fact]
    public void TryPlay_RejectsMovesAfterGameEnded()
    {
        var game = new Game();
        // X: 0,0  0,1  0,2 is a win.
        game.TryPlay(new Move(Symbol.X, new Position(0, 0)));
        game.TryPlay(new Move(Symbol.O, new Position(1, 0)));
        game.TryPlay(new Move(Symbol.X, new Position(0, 1)));
        game.TryPlay(new Move(Symbol.O, new Position(1, 1)));
        game.TryPlay(new Move(Symbol.X, new Position(0, 2)));

        Assert.Equal(GameStatus.XWins, game.Status);
        Assert.False(game.TryPlay(new Move(Symbol.O, new Position(2, 2))));
        Assert.Equal(Symbol.Empty, game[new Position(2, 2)]);
    }

    [Fact]
    public void WinOnlyDetectedForCompleteLine()
    {
        var game = new Game();
        game.TryPlay(new Move(Symbol.X, new Position(0, 0)));
        game.TryPlay(new Move(Symbol.O, new Position(1, 0)));
        game.TryPlay(new Move(Symbol.X, new Position(0, 1)));
        game.TryPlay(new Move(Symbol.O, new Position(1, 1)));
        Assert.Equal(GameStatus.InProgress, game.Status);

        game.TryPlay(new Move(Symbol.X, new Position(0, 2)));
        Assert.Equal(GameStatus.XWins, game.Status);
        Assert.Equal(Symbol.X, game.CurrentTurn); // turn does not advance past a win
    }

    [Fact]
    public void Draw_DetectedOnFullBoard()
    {
        var game = new Game();
        var script = new (Symbol, Position)[]
        {
            (Symbol.X, new Position(0, 0)), (Symbol.O, new Position(0, 1)),
            (Symbol.X, new Position(0, 2)), (Symbol.O, new Position(1, 1)),
            (Symbol.X, new Position(1, 0)), (Symbol.O, new Position(1, 2)),
            (Symbol.X, new Position(2, 2)), (Symbol.O, new Position(2, 0)),
            (Symbol.X, new Position(2, 1)),
        };
        foreach (var (symbol, position) in script)
        {
            Assert.True(game.TryPlay(new Move(symbol, position)));
            if (game.Status != GameStatus.InProgress)
                break;
        }

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Equal(9, game.MoveHistory.Count);
    }

    [Fact]
    public void Reset_ClearsBoardAndHistory()
    {
        var game = new Game();
        game.TryPlay(new Move(Symbol.X, new Position(0, 0)));
        game.TryPlay(new Move(Symbol.O, new Position(1, 1)));

        game.Reset();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Symbol.X, game.CurrentTurn);
        Assert.Empty(game.MoveHistory);
        Assert.All(game.Board.Cells, cell => Assert.Equal(Symbol.Empty, cell.Symbol));
    }
}