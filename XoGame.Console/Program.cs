using XoGame.Bluetooth;
using XoGame.Core.Models;
using XoGame.Core.Play;

namespace XoGame.ConsoleApp;

public static class Program
{
    public static async Task Main()
    {
        while (true)
        {
            var choice = ShowMenu();
            switch (choice)
            {
                case 1: await PlayHotseatAsync(); break;
                case 2: await PlayBluetoothHostAsync(); break;
                case 3: await PlayBluetoothGuestAsync(); break;
                case 4: Console.Clear(); return;
            }
        }
    }

    private static int ShowMenu()
    {
        Console.Clear();
        Console.WriteLine("Tic-Tac-Toe");
        Console.WriteLine("1) Play locally (hotseat)");
        Console.WriteLine("2) Play over Bluetooth  - host (X)");
        Console.WriteLine("3) Play over Bluetooth  - join (O)");
        Console.WriteLine("4) Quit");
        Console.Write("Choose: ");

        return int.TryParse(Console.ReadLine()?.Trim(), out var value) ? value : 0;
    }

    private static async Task PlayHotseatAsync()
    {
        var session = new GameSession(new LocalPlayer(Symbol.X), new LocalPlayer(Symbol.O));
        await PlaySessionAsync(session, "Hotseat");
    }

    private static async Task PlayBluetoothHostAsync()
    {
        try
        {
            var transport = await BluetoothConnection.HostAsync();
            var session = new GameSession(
                new LocalPlayer(Symbol.X),
                new RemotePlayer(Symbol.O, transport),
                transport,
                Symbol.X);

            await PlaySessionAsync(session, "Bluetooth - Host (X)");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Could not host over Bluetooth: {ex.Message}");
        }
    }

    private static async Task PlayBluetoothGuestAsync()
    {
        try
        {
            var address = await PickGuestDeviceAsync();
            if (address is null)
                return;

            var transport = await BluetoothConnection.ConnectAsync(address);
            var session = new GameSession(
                new RemotePlayer(Symbol.X, transport),
                new LocalPlayer(Symbol.O),
                transport,
                Symbol.O);

            await PlaySessionAsync(session, "Bluetooth - Guest (O)");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Could not join over Bluetooth: {ex.Message}");
        }
    }

    private static async Task<string?> PickGuestDeviceAsync()
    {
        Console.WriteLine("Scanning for nearby devices...");
        var devices = await BluetoothConnection.DiscoverAsync();

        if (devices.Count == 0)
        {
            Console.WriteLine("No discoverable devices were found.");
            Console.Write("Type a Bluetooth address manually or press Enter to cancel: ");
            return Console.ReadLine()?.Trim();
        }

        for (var i = 0; i < devices.Count; i++)
            Console.WriteLine($"{i + 1}) {devices[i]}");

        Console.Write("Pick a device, or type an address manually, or press Enter to cancel: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
            return null;

        if (int.TryParse(input, out var index) && index >= 1 && index <= devices.Count)
            return devices[index - 1].Address;

        return input;
    }

    private static async Task PlaySessionAsync(GameSession session, string title)
    {
        var renderer = new GameRenderer(title);
        var disconnected = false;
        var quit = false;

        void Render() => renderer.Render(session.Game, BuildStatus(session, disconnected));

        session.Game.CellChanged += (_, _) => Render();
        session.Game.TurnChanged += (_, _) => Render();
        session.Game.StatusChanged += (_, _) => Render();
        session.Disconnected += (_, _) => disconnected = true;

        _ = session.RunAsync();

        var replay = true;
        while (replay)
        {
            replay = false;
            var movesBefore = session.Game.MoveHistory.Count;

            while (session.Game.Status == GameStatus.InProgress && !disconnected && !quit)
            {
                renderer.Render(session.Game, BuildStatus(session, disconnected));

                if (session.CanPlayLocally)
                {
                    var input = renderer.ReadInput(
                        $"Move for {session.LocalPlayerSymbolOrCurrent} (1-9), (r)eset, (q)uit: ").Trim().ToLowerInvariant();

                    switch (input)
                    {
                        case "r":
                            await session.ResetAsync();
                            movesBefore = session.Game.MoveHistory.Count;
                            break;

                        case "q":
                            quit = true;
                            break;

                        case { Length: 1 } when input[0] is >= '1' and <= '9':
                            session.SubmitMove(Position.FromIndex(input[0] - '1'));
                            await WaitAppliedAsync(session, movesBefore, () => disconnected || quit);
                            movesBefore = session.Game.MoveHistory.Count;
                            break;

                        default:
                            renderer.Message("Type a cell number 1-9, r to reset, q to quit.");
                            break;
                    }
                }
                else
                {
                    // Opponent's turn (or waiting for the peer): re-render on events, polling the board.
                    Thread.Sleep(150);
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                        quit = true;
                }
            }

            renderer.Render(session.Game, BuildStatus(session, disconnected));

            if (disconnected || quit)
            {
                renderer.Message(disconnected ? "Opponent disconnected - returning to the menu."
                                              : "You quit - returning to the menu.");
            }
            else if (renderer.ReadInput("(p)lay again, (m)enu: ").Trim().ToLowerInvariant() == "p")
            {
                await session.ResetAsync();
                replay = true;
            }
        }

        renderer.Cleanup();
        await session.DisposeAsync();
    }

    /// <summary>
    /// The session applies moves on a background loop, so a submitted tap must not arrive
    /// faster than the board can absorb it - otherwise moves would be routed to the wrong
    /// player. This paces the UI exactly like the tests' WaitUntilAsync helper.
    /// </summary>
    private static async Task WaitAppliedAsync(GameSession session, int movesBefore, Func<bool> stop)
    {
        while (!stop() && session.Game.Status == GameStatus.InProgress
               && session.Game.MoveHistory.Count == movesBefore)
        {
            await Task.Delay(15);
        }
    }

    private static string BuildStatus(GameSession session, bool disconnected)
    {
        var game = session.Game;
        var local = session.LocalSymbol;

        if (disconnected)
            return "Opponent disconnected.";

        return game.Status switch
        {
            GameStatus.InProgress when local is null => $"It's {game.CurrentTurn}'s turn.",
            GameStatus.InProgress when game.CurrentTurn == local => $"Your turn ({local}).",
            GameStatus.InProgress => "Waiting for the opponent...",
            GameStatus.XWins when local is null => "X wins!",
            GameStatus.XWins when local == Symbol.X => "You win! (X)",
            GameStatus.XWins => "Opponent wins (X).",
            GameStatus.OWins when local is null => "O wins!",
            GameStatus.OWins when local == Symbol.O => "You win! (O)",
            GameStatus.OWins => "Opponent wins (O).",
            _ => "It's a draw!",
        };
    }
}