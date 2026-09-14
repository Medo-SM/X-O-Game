using XoGame.Core.Models;
using XoGame.Core.Play;
using XoGame.Core.Transport;
using Xunit;

namespace XoGame.Tests;

public class GameSessionTests
{
    [Fact]
    public async Task Hotseat_FullGame_EndsWithWin()
    {
        await using var session = new GameSession(new LocalPlayer(Symbol.X), new LocalPlayer(Symbol.O));
        var run = session.RunAsync();

        await PlayHotseatAsync(session, new Position(0, 0), 1); // X
        await PlayHotseatAsync(session, new Position(1, 0), 2); // O
        await PlayHotseatAsync(session, new Position(0, 1), 3); // X
        await PlayHotseatAsync(session, new Position(1, 1), 4); // O
        await PlayHotseatAsync(session, new Position(0, 2), 5); // X wins

        await WaitForEndAsync(session);

        Assert.Equal(GameStatus.XWins, session.Game.Status);
        Assert.Equal(5, session.Game.MoveHistory.Count);
        Assert.Null(session.LocalSymbol);

        await DisposeAsync(session, run);
    }

    [Fact]
    public async Task Hotseat_ResetClearsEverything()
    {
        await using var session = new GameSession(new LocalPlayer(Symbol.X), new LocalPlayer(Symbol.O));
        var run = session.RunAsync();

        await PlayHotseatAsync(session, new Position(0, 0), 1);

        await session.ResetAsync();

        Assert.Equal(GameStatus.InProgress, session.Game.Status);
        Assert.Empty(session.Game.MoveHistory);
        Assert.All(session.Game.Board.Cells, cell => Assert.Equal(Symbol.Empty, cell.Symbol));

        await DisposeAsync(session, run);
    }

    [Fact]
    public async Task SimulatedBluetooth_FullMatch_BothSidesAgree()
    {
        var (host, client, _, _) = CreatePair();
        var hostRun = host.RunAsync();
        var clientRun = client.RunAsync();

        // Host plays X, client plays O - each move is committed only once the other side
        // has received it, exactly like two humans alternating turns over a link.
        await PlayRemoteAsync(host, client, Symbol.X, new Position(0, 0), 1);
        await PlayRemoteAsync(host, client, Symbol.O, new Position(1, 0), 2);
        await PlayRemoteAsync(host, client, Symbol.X, new Position(0, 1), 3);
        await PlayRemoteAsync(host, client, Symbol.O, new Position(1, 1), 4);
        await PlayRemoteAsync(host, client, Symbol.X, new Position(0, 2), 5);

        await WaitForEndAsync(host, client);

        Assert.Equal(GameStatus.XWins, host.Game.Status);
        Assert.Equal(GameStatus.XWins, client.Game.Status);

        // Both boards are identical, whichever side you look at.
        for (var i = 0; i < Position.CellCount; i++)
        {
            var p = Position.FromIndex(i);
            Assert.Equal(host.Game[p], client.Game[p]);
        }

        Assert.Equal(host.Game.MoveHistory, client.Game.MoveHistory);

        // A late X move after the game ended is rejected by both sides.
        host.SubmitMove(new Position(2, 0));
        await Task.Delay(50);
        Assert.Equal(Symbol.Empty, client.Game[new Position(2, 0)]);

        await DisposeAsync(host, client, hostRun, clientRun);
    }

    [Fact]
    public async Task SimulatedBluetooth_CanPlayLocallyFollowsTurn()
    {
        var (host, client, _, _) = CreatePair();
        var hostRun = host.RunAsync();
        var clientRun = client.RunAsync();

        Assert.True(host.CanPlayLocally);      // X's turn, host has X
        Assert.False(client.CanPlayLocally);   // X's turn, client has O

        await PlayRemoteAsync(host, client, Symbol.X, new Position(0, 0), 1);

        Assert.False(host.CanPlayLocally);     // now O's turn - remote for the host
        Assert.True(client.CanPlayLocally);

        await DisposeAsync(host, client, hostRun, clientRun);
    }

    [Fact]
    public async Task SimulatedBluetooth_RematchAfterWin()
    {
        var (host, client, _, _) = CreatePair();
        var hostRun = host.RunAsync();
        var clientRun = client.RunAsync();

        await PlayRemoteAsync(host, client, Symbol.X, new Position(0, 0), 1);
        await PlayRemoteAsync(host, client, Symbol.O, new Position(1, 0), 2);
        await PlayRemoteAsync(host, client, Symbol.X, new Position(0, 1), 3);
        await PlayRemoteAsync(host, client, Symbol.O, new Position(1, 1), 4);
        await PlayRemoteAsync(host, client, Symbol.X, new Position(0, 2), 5);

        await WaitForEndAsync(host, client);

        // The loser asks for a rematch; the winner was idling and must honor the request.
        await client.ResetAsync();

        await WaitForResetAsync(host, client);

        Assert.All(host.Game.Board.Cells, cell => Assert.Equal(Symbol.Empty, cell.Symbol));
        Assert.All(client.Game.Board.Cells, cell => Assert.Equal(Symbol.Empty, cell.Symbol));

        // Play a second round through the same pair.
        await PlayRemoteAsync(host, client, Symbol.X, new Position(1, 1), 1);
        await PlayRemoteAsync(host, client, Symbol.O, new Position(0, 0), 2);
        await PlayRemoteAsync(host, client, Symbol.X, new Position(1, 0), 3);
        await PlayRemoteAsync(host, client, Symbol.O, new Position(0, 1), 4);
        await PlayRemoteAsync(host, client, Symbol.X, new Position(1, 2), 5);

        await WaitForEndAsync(host, client);
        Assert.Equal(GameStatus.XWins, host.Game.Status);
        Assert.Equal(GameStatus.XWins, client.Game.Status);

        await DisposeAsync(host, client, hostRun, clientRun);
    }

    [Fact]
    public async Task SimulatedBluetooth_DisconnectStopsTheIdleSide()
    {
        var (host, client, hostToClient, _) = CreatePair();

        var hostRun = host.RunAsync();
        var clientRun = client.RunAsync();
        var clientDisconnected = new TaskCompletionSource();

        client.Disconnected += (_, _) => clientDisconnected.TrySetResult();

        await PlayRemoteAsync(host, client, Symbol.X, new Position(0, 0), 1);
        await PlayRemoteAsync(host, client, Symbol.O, new Position(1, 0), 2);

        // Simulate the host vanishing: the host->client link dies mid-await.
        await hostToClient.DisposeAsync();

        var completed = await Task.WhenAny(clientDisconnected.Task, Task.Delay(2000));
        Assert.Same(clientDisconnected.Task, completed);
        Assert.True(clientDisconnected.Task.IsCompleted, "Client should have been notified of the disconnect.");

        await DisposeAsync(host, client, hostRun, clientRun);
    }

    private static async Task PlayHotseatAsync(GameSession session, Position position, int expectedHistory)
    {
        session.SubmitMove(position);
        await TestHelpers.WaitUntilAsync(() => session.Game.MoveHistory.Count == expectedHistory);
    }

    private static async Task PlayRemoteAsync(GameSession host, GameSession client, Symbol symbol, Position position, int expectedHistory)
    {
        (symbol == Symbol.X ? host : client).SubmitMove(position);
        await TestHelpers.WaitUntilAsync(() => host.Game.MoveHistory.Count == expectedHistory);
        await TestHelpers.WaitUntilAsync(() => client.Game.MoveHistory.Count == expectedHistory);
    }

    private static Task WaitForEndAsync(GameSession session)
        => TestHelpers.WaitUntilAsync(() => session.Game.Status != GameStatus.InProgress);

    private static Task WaitForEndAsync(GameSession host, GameSession client)
        => Task.WhenAll(WaitForEndAsync(host), WaitForEndAsync(client));

    private static async Task WaitForResetAsync(GameSession host, GameSession client)
    {
        await TestHelpers.WaitUntilAsync(() => host.Game.Status == GameStatus.InProgress);
        await TestHelpers.WaitUntilAsync(() => client.Game.Status == GameStatus.InProgress);
        await TestHelpers.WaitUntilAsync(() => host.Game.MoveHistory.Count == 0);
        await TestHelpers.WaitUntilAsync(() => client.Game.MoveHistory.Count == 0);
    }

    private static (GameSession Host, GameSession Client, InProcessTransport HostToClient, InProcessTransport ClientToHost) CreatePair()
    {
        var hostToClient = new InProcessTransport();
        var clientToHost = new InProcessTransport();

        var host = new GameSession(
            new LocalPlayer(Symbol.X),
            new RemotePlayer(Symbol.O, clientToHost),
            hostToClient,
            Symbol.X);

        var client = new GameSession(
            new RemotePlayer(Symbol.X, hostToClient),
            new LocalPlayer(Symbol.O),
            clientToHost,
            Symbol.O);

        return (host, client, hostToClient, clientToHost);
    }

    private static async Task DisposeAsync(GameSession session, Task run)
    {
        try { await session.DisposeAsync().AsTask(); } catch { }
        try { await run; } catch { }
    }

    private static async Task DisposeAsync(GameSession first, GameSession second, Task firstRun, Task secondRun)
    {
        await DisposeAsync(first, firstRun);
        await DisposeAsync(second, secondRun);
    }
}