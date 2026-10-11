namespace Isaac.Core;

/// <summary>Global constants shared by simulation, networking and rendering.</summary>
public static class Config
{
    // Room geometry (Isaac rooms are 13x7 tiles).
    public const int Tile = 64;
    public const int RoomTilesW = 13;
    public const int RoomTilesH = 7;
    public const int RoomW = RoomTilesW * Tile;
    public const int RoomH = RoomTilesH * Tile;

    // Screen layout.
    public const int Wall = 64;
    public const int HudHeight = 72;
    public const int ScreenW = RoomW + Wall * 2;
    public const int ScreenH = HudHeight + RoomH + Wall * 2;

    // Simulation.
    public const int TickRate = 60;
    public const float TickDt = 1f / TickRate;

    // Every Nth floor is a boss rush instead of a normal floor.
    public const int BossRushEvery = 3;

    // Networking.
    public const int DefaultPort = 7777;
    public const string ConnectionKey = "isaac-coop-v1";
    public const int MaxPlayers = 4;
    public static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan DisconnectTimeout = TimeSpan.FromSeconds(5);
}
