namespace XoGame.Core.Models;

/// <summary>The three aligned positions that decide a game, if any.</summary>
public sealed record WinLine(Symbol Symbol, IReadOnlyList<Position> Positions);