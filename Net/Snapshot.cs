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
/// tick; the host renders it directly and also sends it to every client.
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
    public readonly List<byte> Rocks = new();
    public readonly List<PlayerView> Players = new();
    public readonly List<EnemyView> Enemies = new();
    public readonly List<TearView> Tears = new();
    public readonly List<PickupView> Pickups = new();
    public readonly List<MapCell> Map = new();

    public bool HasDoor(Direction d) => (DoorMask & (1 << (int)d)) != 0;

    public void Write(BinaryWriter w)
    {
        w.Write(Tick);
        w.Write(Floor);
        w.Write(Score);
        w.Write(GameOver);
        w.Write((byte)RoomKind);
        w.Write(DoorMask);
        w.Write(DoorsOpen);
        w.Write(Message);

        w.Write((byte)Rocks.Count);
        foreach (var r in Rocks) w.Write(r);

        w.Write((byte)Players.Count);
        foreach (var p in Players)
        {
            w.Write(p.Id); w.Write(p.X); w.Write(p.Y); w.Write(p.LookX); w.Write(p.LookY);
            w.Write(p.Health); w.Write(p.MaxHealth); w.Write(p.Blink);
            w.Write(p.Damage); w.Write(p.Speed); w.Write(p.TearsPerSecond); w.Write(p.ItemCount);
        }

        w.Write((byte)Enemies.Count);
        foreach (var e in Enemies)
        {
            w.Write((byte)e.Kind); w.Write(e.X); w.Write(e.Y); w.Write(e.Radius); w.Write(e.HpPercent); w.Write(e.Flash);
        }

        int tearCount = Math.Min(Tears.Count, ushort.MaxValue);
        w.Write((ushort)tearCount);
        for (int i = 0; i < tearCount; i++)
        {
            var t = Tears[i];
            w.Write(t.X); w.Write(t.Y); w.Write(t.Radius); w.Write(t.FromEnemy);
        }

        w.Write((byte)Pickups.Count);
        foreach (var p in Pickups)
        {
            w.Write((byte)p.Kind); w.Write(p.X); w.Write(p.Y); w.Write((byte)p.Item);
        }

        w.Write((byte)Map.Count);
        foreach (var m in Map)
        {
            w.Write(m.X); w.Write(m.Y); w.Write((byte)m.Kind); w.Write(m.Flags);
        }
    }

    public static Snapshot Read(BinaryReader r)
    {
        var s = new Snapshot
        {
            Tick = r.ReadUInt32(),
            Floor = r.ReadByte(),
            Score = r.ReadInt32(),
            GameOver = r.ReadBoolean(),
            RoomKind = (RoomKind)r.ReadByte(),
            DoorMask = r.ReadByte(),
            DoorsOpen = r.ReadBoolean(),
            Message = r.ReadString(),
        };

        for (int i = r.ReadByte(); i > 0; i--) s.Rocks.Add(r.ReadByte());

        for (int i = r.ReadByte(); i > 0; i--)
        {
            s.Players.Add(new PlayerView
            {
                Id = r.ReadByte(), X = r.ReadInt16(), Y = r.ReadInt16(), LookX = r.ReadSByte(), LookY = r.ReadSByte(),
                Health = r.ReadByte(), MaxHealth = r.ReadByte(), Blink = r.ReadBoolean(),
                Damage = r.ReadSingle(), Speed = r.ReadSingle(), TearsPerSecond = r.ReadSingle(), ItemCount = r.ReadByte(),
            });
        }

        for (int i = r.ReadByte(); i > 0; i--)
        {
            s.Enemies.Add(new EnemyView
            {
                Kind = (EnemyKind)r.ReadByte(), X = r.ReadInt16(), Y = r.ReadInt16(),
                Radius = r.ReadByte(), HpPercent = r.ReadByte(), Flash = r.ReadBoolean(),
            });
        }

        for (int i = r.ReadUInt16(); i > 0; i--)
            s.Tears.Add(new TearView { X = r.ReadInt16(), Y = r.ReadInt16(), Radius = r.ReadByte(), FromEnemy = r.ReadBoolean() });

        for (int i = r.ReadByte(); i > 0; i--)
            s.Pickups.Add(new PickupView { Kind = (PickupKind)r.ReadByte(), X = r.ReadInt16(), Y = r.ReadInt16(), Item = (ItemKind)r.ReadByte() });

        for (int i = r.ReadByte(); i > 0; i--)
            s.Map.Add(new MapCell { X = r.ReadSByte(), Y = r.ReadSByte(), Kind = (RoomKind)r.ReadByte(), Flags = r.ReadByte() });

        return s;
    }
}
