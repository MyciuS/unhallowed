using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Net;

public struct PlayerView
{
    public byte Id;
    public short X, Y;
    public sbyte LookX, LookY;
    public byte Health, MaxHealth;
    public bool Blink;
    public float Damage, Speed, TearsPerSecond;
    public byte ItemCount;
    public string Character;
}

public struct EnemyView
{
    public EnemyKind Kind;
    public short X, Y;
    public byte Radius;
    public byte HpPercent;
    public bool Flash;
}

public struct TearView
{
    public short X, Y;
    public byte Radius;
    public bool FromEnemy;
}

public struct PickupView
{
    public PickupKind Kind;
    public short X, Y;
    public ItemKind Item;
}

public struct MapCell
{
    public const byte FlagVisited = 1, FlagCurrent = 2;
    public sbyte X, Y;
    public RoomKind Kind;
    public byte Flags;
}

/// <summary>
/// Everything a client needs to draw one frame. Built by the host's World each
/// tick; the host renders it directly and also sends it to every client as JSON.
/// </summary>
public sealed class Snapshot
{
    public uint Tick;
    public byte Floor;
    public int Score;
    public bool GameOver;
    public RoomKind RoomKind;
    public byte DoorMask;
    public bool DoorsOpen;
    public string Message = "";
    public List<byte> Rocks = new();
    public List<PlayerView> Players = new();
    public List<EnemyView> Enemies = new();
    public List<TearView> Tears = new();
    public List<PickupView> Pickups = new();
    public List<MapCell> Map = new();

    public bool HasDoor(Direction d) => (DoorMask & (1 << (int)d)) != 0;
}
