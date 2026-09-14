# Tic-Tac-Toe (XoGame)

A Tic-Tac-Toe game written in C# using object-oriented programming. It can be played in
three ways:

1. **Hotseat** - two players take turns on the same device.
2. **Bluetooth host (X)** - one device waits for another device to connect, then they play.
3. **Bluetooth join (O)** - a device finds the host and connects to it.

The same game engine powers every mode; Bluetooth is just another way to receive the
opponent's moves.

## How to run

Requires the .NET 10 SDK.

| Want | Command | Where it runs |
|---|---|---|
| Window with clickable squares | `dotnet run --project XoGame.App` | Windows |
| Terminal board | `dotnet run --project XoGame.Console` | Linux / macOS / Windows |
| Unit tests | `dotnet test XoGame.Tests/XoGame.Tests.csproj` | anywhere |

Build everything: `dotnet build XoGame.slnx`

## Projects

* **XoGame.Core** - the game engine, with no user interface.
    * `Models` - `Symbol` (X/O/empty), `Position` (row + column), `Cell`, `Board` (the 3x3
      grid, whose squares only it can change), `WinConditionChecker` (detects a full line),
      `GameStatus`, and `Game` (turns, legality, win/draw, events).
    * `Play` - `Player` (base class: "where does the next move come from?"), `LocalPlayer`
      (human taps), `RemotePlayer` (moves from the transport), and `GameSession`, which asks
      the right player for a move, applies it and relays it.
    * `Transport` - `ITransport` (send/receive), `Command` + `MoveCodec` (the small message
      format used over Bluetooth), and `InProcessTransport` (an in-memory pipe used by tests
      to simulate a Bluetooth match).
* **XoGame.Bluetooth** - connects two devices together (host, join, device discovery) and
  exposes the result as an `ITransport`, so the engine does not know (or care) that the move
  travelled over Bluetooth. This is the key OOP idea: the UI/engine stay decoupled from the
  transport.
* **XoGame.App** - the WinForms GUI. `MainForm` picks the mode, `JoinDialog`/`WaitingForm`
  handle the Bluetooth setup, `GameForm` draws the 3x3 grid, and `GameController` reacts to
  the engine's events and refreshes the window.
* **XoGame.Console** - a terminal version with the exact same engine calls.
* **XoGame.Tests** - xUnit tests for the rules, the win check, the message format and full
  simulated Bluetooth matches.

## OOP ideas shown

* **Encapsulation** - `Board` changes its cells itself; `Cell.Symbol` only has an internal
  setter, so a player cannot cheat by placing a piece directly.
* **Classes with a single job** - `Board` stores the grid, `WinConditionChecker` checks wins,
  `MoveCodec` packs messages, `GameSession` orchestrates.
* **Inheritance + polymorphism** - the abstract `Player` base class with `LocalPlayer` and
  `RemotePlayer` subclasses; the session just calls `GetMoveAsync()` without caring who it is.
* **Separation of concerns** - engine, transport and user interface are separate projects
  that talk through small interfaces (`ITransport`, `Game` events).