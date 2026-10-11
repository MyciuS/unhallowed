using System.Numerics;
using Isaac.Core;
using Isaac.Game.Entities;

namespace Isaac.Game.Dungeon;

public enum RoomKind : byte
{
    Normal,
    Start,
    Treasure,
    Boss,
}

public enum Direction
{
    Up,
    Right,
    Down,
    Left,
}

public static class DirectionExt
{
    public static (int dx, int dy) Offset(this Direction d) => d switch
    {
        Direction.Up => (0, -1),
        Direction.Right => (1, 0),
        Direction.Down => (0, 1),
        _ => (-1, 0),
    };

    public static Direction Opposite(this Direction d) => (Direction)(((int)d + 2) % 4);
}

public sealed class Room(int gx, int gy)
{
    public int GridX { get; } = gx;
    public int GridY { get; } = gy;
    public RoomKind Kind = RoomKind.Normal;
    public readonly bool[] Doors = new bool[4];
    public readonly bool[,] Rocks = new bool[Config.RoomTilesW, Config.RoomTilesH];
    public readonly List<EnemySpawn> Spawns = new();
    public readonly List<Pickup> Pickups = new();
    public bool Cleared;
    public bool Visited;

    public bool HasDoor(Direction d) => Doors[(int)d];

    public bool IsSolidTile(int tx, int ty) =>
        tx < 0 || ty < 0 || tx >= Config.RoomTilesW || ty >= Config.RoomTilesH || Rocks[tx, ty];

    public static Vector2 TileCenter(int tx, int ty) =>
        new((tx + 0.5f) * Config.Tile, (ty + 0.5f) * Config.Tile);

    /// <summary>Point just inside the room next to a door.</summary>
    public static Vector2 DoorEntry(Direction d) => d switch
    {
        Direction.Up => new(Config.RoomW / 2f, 40),
        Direction.Down => new(Config.RoomW / 2f, Config.RoomH - 40),
        Direction.Left => new(40, Config.RoomH / 2f),
        _ => new(Config.RoomW - 40, Config.RoomH / 2f),
    };

    public static Vector2 Center => new(Config.RoomW / 2f, Config.RoomH / 2f);
}

/// <summary>BUILDER pattern, the Product: one floor of rooms, made by an <see cref="IFloorBuilder"/>.</summary>
public sealed class Floor(
    int number,
    string title,
    Dictionary<(int, int), Room> rooms,
    Room start,
    IEnemyFactory enemyFactory)
{
    public int Number { get; } = number;
    public string Title { get; } = title;
    public IReadOnlyDictionary<(int, int), Room> Rooms { get; } = rooms;
    public Room Start { get; } = start;
    public IEnemyFactory EnemyFactory { get; } = enemyFactory;

    public Room? Neighbor(Room room, Direction d)
    {
        var (dx, dy) = d.Offset();
        return Rooms.GetValueOrDefault((room.GridX + dx, room.GridY + dy));
    }

    public int BossRoomsLeft => Rooms.Values.Count(r => r.Kind == RoomKind.Boss && !r.Cleared);
}
