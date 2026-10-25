using System.Numerics;
using Isaac.Core;
using Isaac.Game.Characters;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Net;

namespace Isaac.Game;

/// <summary>
/// The authoritative game simulation. Only the host runs it; clients just
/// render the <see cref="Snapshot"/>s it produces.
/// All players share one room at a time (like Isaac co-op).
/// </summary>
public sealed class World
{
    private readonly ScoreKeeper _score;
    private readonly FloorDirector _director = new();
    private readonly IFloorBuilder _standardBuilder = new StandardFloorBuilder();
    private readonly IFloorBuilder _bossRushBuilder = new BossRushFloorBuilder();
    private readonly CharacterRegistry _characters = CharacterRegistry.CreateDefault();
    private float _messageTimer;
    private float _transitionCooldown;

    public World()
    {
        _score = new ScoreKeeper(Events);
        _ = new Announcer(this, Events);
        _ = new LootDropper(Events);
        _ = new ActionLogger(Events);
        StartFloor(1);
    }

    public EventBus Events { get; } = new();
    public Floor Floor { get; private set; } = null!;
    public Room Room { get; private set; } = null!;
    public Dictionary<int, Player> Players { get; } = new();
    public List<Enemy> Enemies { get; } = new();
    public List<Tear> Tears { get; } = new();
    public uint Tick { get; private set; }
    public string Message { get; private set; } = "";
    public bool GameOver { get; private set; }
    public int Score => _score.Score;

    // ---------------------------------------------------------------- players

    public Player AddPlayer(int id)
    {
        var p = new Player(id, _characters.CreateForPlayer(id))
        {
            Pos = Room.Center + new Vector2((id - 1.5f) * 50, 60),
        };
        Players[id] = p;
        return p;
    }

    public void RemovePlayer(int id) => Players.Remove(id);

    public Player? NearestPlayer(Vector2 pos)
    {
        Player? best = null;
        float bestDist = float.MaxValue;
        foreach (var p in Players.Values)
        {
            if (!p.Alive)
                continue;
            float d = Vector2.DistanceSquared(p.Pos, pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }

    public void ShowMessage(string text)
    {
        Message = text;
        _messageTimer = 3f;
    }

    // ------------------------------------------------------------- lifecycle

    public void Restart()
    {
        var ids = Players.Keys.ToList();
        Players.Clear();
        _score.Reset();
        GameOver = false;
        StartFloor(1);
        foreach (int id in ids)
            AddPlayer(id);
    }

    private void StartFloor(int number)
    {
        var builder = number % Config.BossRushEvery == 0 ? _bossRushBuilder : _standardBuilder;
        Floor = _director.Construct(builder, number);
        EnterRoom(Floor.Start, null);
        int i = 0;
        foreach (var p in Players.Values)
        {
            // Reaching a new floor revives fallen co-op partners.
            if (!p.Alive)
                p.Health = Math.Min(4, p.Stats.MaxHealth);
            p.Pos = Room.Center + new Vector2((i++ - 1.5f) * 50, 60);
        }
        Events.Publish(new FloorStarted(Floor));
    }

    private void EnterRoom(Room room, Direction? enteredThrough)
    {
        Room = room;
        room.Visited = true;
        Tears.Clear();
        Enemies.Clear();
        if (!room.Cleared)
            foreach (var spawn in room.Spawns)
                Enemies.Add(Floor.EnemyFactory.Create(spawn, Floor.Number));

        if (enteredThrough is { } dir)
        {
            var entry = Room.DoorEntry(dir.Opposite());
            var side = dir is Direction.Up or Direction.Down ? Vector2.UnitX : Vector2.UnitY;
            int i = 0;
            foreach (var p in Players.Values)
                p.Pos = entry + side * ((i++ - (Players.Count - 1) / 2f) * 48);
        }
        _transitionCooldown = 0.3f;
    }

    // ------------------------------------------------------------ simulation

    public void Update(float dt)
    {
        Tick++;
        if (_messageTimer > 0 && (_messageTimer -= dt) <= 0)
            Message = "";
        if (GameOver)
            return;
        _transitionCooldown -= dt;

        UpdatePlayers(dt);
        UpdateEnemies(dt);
        UpdateTears(dt);
        UpdatePickups();

        if (!Room.Cleared && Enemies.Count == 0)
        {
            Room.Cleared = true;
            Events.Publish(new RoomCleared(Room, Floor));
        }

        if (Players.Count > 0 && Players.Values.All(p => !p.Alive))
        {
            GameOver = true;
            return;
        }

        CheckDoors();
    }

    private void UpdatePlayers(float dt)
    {
        foreach (var p in Players.Values)
        {
            if (!p.Alive)
                continue;
            p.Invuln = MathF.Max(0, p.Invuln - dt);
            p.FireCooldown -= dt;

            var move =
                p.MoveIntent == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(p.MoveIntent);
            // Slight acceleration so movement feels less robotic.
            p.Vel = Vector2.Lerp(p.Vel, move * p.Stats.Speed, MathF.Min(1, dt * 14));
            p.Pos = MoveBody(p.Pos, Player.Radius, p.Vel * dt, ignoreRocks: false);

            if (p.ShootIntent != Vector2.Zero)
            {
                var dir = Vector2.Normalize(p.ShootIntent);
                p.LookDir = dir;
                if (p.FireCooldown <= 0)
                {
                    p.FireCooldown = p.Stats.FireDelay;
                    var vel = dir * p.Stats.ShotSpeed + p.Vel * 0.3f;
                    SpawnTear(
                        p.Pos + dir * 10,
                        vel,
                        p.Stats.Damage,
                        p.Stats.TearSize,
                        p.Stats.Range,
                        fromEnemy: false
                    );
                }
            }
            else if (move != Vector2.Zero)
            {
                p.LookDir = move;
            }
        }
    }

    private void UpdateEnemies(float dt)
    {
        foreach (var e in Enemies)
            e.Update(this, dt);

        // Simple separation so enemies do not stack on top of each other.
        for (int i = 0; i < Enemies.Count; i++)
        for (int j = i + 1; j < Enemies.Count; j++)
        {
            var a = Enemies[i];
            var b = Enemies[j];
            var d = b.Pos - a.Pos;
            float min = a.Radius + b.Radius;
            float len = d.Length();
            if (len >= min || len < 0.001f)
                continue;
            var push = d / len * (min - len) * 0.5f;
            if (!a.IsBoss)
                a.Pos = MoveBody(a.Pos, a.Radius, -push, a.Flying);
            if (!b.IsBoss)
                b.Pos = MoveBody(b.Pos, b.Radius, push, b.Flying);
        }

        // Contact damage.
        foreach (var e in Enemies)
        foreach (var p in Players.Values)
        {
            if (Vector2.Distance(e.Pos, p.Pos) < e.Radius + Player.Radius - 4)
                HurtPlayer(p, e.IsBoss ? 2 : 1);
        }
    }

    private void UpdateTears(float dt)
    {
        foreach (var t in Tears)
        {
            t.Pos += t.Vel * dt;
            t.Life -= dt;
            if (t.Life <= 0 || IsSolid(t.Pos))
            {
                t.Dead = true;
                continue;
            }

            if (t.FromEnemy)
            {
                foreach (var p in Players.Values)
                {
                    if (p.Alive && Vector2.Distance(p.Pos, t.Pos) < Player.Radius + t.Radius - 4)
                    {
                        HurtPlayer(p, 1);
                        t.Dead = true;
                        break;
                    }
                }
            }
            else
            {
                foreach (var e in Enemies)
                {
                    if (e.Dead || Vector2.Distance(e.Pos, t.Pos) >= e.Radius + t.Radius)
                        continue;
                    e.TakeDamage(t.Damage);
                    if (!e.IsBoss)
                        e.Pos = MoveBody(e.Pos, e.Radius, Vector2.Normalize(t.Vel) * 10, e.Flying);
                    t.Dead = true;
                    break;
                }
            }
        }

        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            if (!Enemies[i].Dead)
                continue;
            var dead = Enemies[i];
            Enemies.RemoveAt(i);
            Events.Publish(new EnemyKilled(dead));
        }

        Tears.RemoveAll(t => t.Dead);
    }

    private void UpdatePickups()
    {
        for (int i = Room.Pickups.Count - 1; i >= 0; i--)
        {
            var pickup = Room.Pickups[i];
            foreach (var p in Players.Values)
            {
                if (!p.Alive || Vector2.Distance(p.Pos, pickup.Pos) > Player.Radius + pickup.Radius)
                    continue;
                switch (pickup.Kind)
                {
                    case PickupKind.Heart when p.Health < p.Stats.MaxHealth:
                        p.Health = Math.Min(p.Stats.MaxHealth, p.Health + 2);
                        Room.Pickups.RemoveAt(i);
                        break;
                    case PickupKind.Item:
                        p.GiveItem(pickup.Item);
                        Room.Pickups.RemoveAt(i);
                        Events.Publish(new ItemPickedUp(p, pickup.Item));
                        break;
                    case PickupKind.Trapdoor when _transitionCooldown <= 0:
                        StartFloor(Floor.Number + 1);
                        return;
                    default:
                        continue;
                }
                break;
            }
        }
    }

    private void CheckDoors()
    {
        if (!Room.Cleared || _transitionCooldown > 0)
            return;
        foreach (var p in Players.Values)
        {
            if (!p.Alive)
                continue;
            Direction? exit = null;
            const float edge = Player.Radius + 2;
            if (MathF.Abs(p.Pos.X - Config.RoomW / 2f) < 40)
            {
                if (p.Pos.Y <= edge && p.MoveIntent.Y < 0)
                    exit = Direction.Up;
                else if (p.Pos.Y >= Config.RoomH - edge && p.MoveIntent.Y > 0)
                    exit = Direction.Down;
            }
            if (MathF.Abs(p.Pos.Y - Config.RoomH / 2f) < 40)
            {
                if (p.Pos.X <= edge && p.MoveIntent.X < 0)
                    exit = Direction.Left;
                else if (p.Pos.X >= Config.RoomW - edge && p.MoveIntent.X > 0)
                    exit = Direction.Right;
            }
            if (exit is { } dir && Room.HasDoor(dir) && Floor.Neighbor(Room, dir) is { } next)
            {
                EnterRoom(next, dir);
                return;
            }
        }
    }

    private void HurtPlayer(Player p, int amount)
    {
        if (!p.TryDamage(amount))
            return;
        Events.Publish(new PlayerHurt(p));
        if (!p.Alive)
            Events.Publish(new PlayerDied(p));
    }

    public void SpawnTear(
        Vector2 pos,
        Vector2 vel,
        float damage,
        float radius,
        float life,
        bool fromEnemy
    )
    {
        Tears.Add(
            new Tear
            {
                Pos = pos,
                Vel = vel,
                Damage = damage,
                Radius = radius,
                Life = life,
                FromEnemy = fromEnemy,
            }
        );
    }

    // ------------------------------------------------------------- collision

    private bool IsSolid(Vector2 pos) =>
        pos.X < 0
        || pos.Y < 0
        || pos.X >= Config.RoomW
        || pos.Y >= Config.RoomH
        || Room.Rocks[(int)(pos.X / Config.Tile), (int)(pos.Y / Config.Tile)];

    private bool HitsRock(Vector2 pos, float r)
    {
        r *= 0.85f; // a bit of leniency so bodies slide around corners
        int x0 = (int)MathF.Floor((pos.X - r) / Config.Tile),
            x1 = (int)MathF.Floor((pos.X + r) / Config.Tile);
        int y0 = (int)MathF.Floor((pos.Y - r) / Config.Tile),
            y1 = (int)MathF.Floor((pos.Y + r) / Config.Tile);
        for (int x = x0; x <= x1; x++)
        for (int y = y0; y <= y1; y++)
            if (
                x >= 0
                && y >= 0
                && x < Config.RoomTilesW
                && y < Config.RoomTilesH
                && Room.Rocks[x, y]
            )
                return true;
        return false;
    }

    /// <summary>Moves a circular body, resolving each axis separately against rocks and room walls.</summary>
    public Vector2 MoveBody(Vector2 pos, float r, Vector2 delta, bool ignoreRocks)
    {
        var next = new Vector2(pos.X + delta.X, pos.Y);
        if (!ignoreRocks && HitsRock(next, r))
            next.X = pos.X;
        next.Y += delta.Y;
        if (!ignoreRocks && HitsRock(next, r))
            next.Y = pos.Y;
        next.X = Math.Clamp(next.X, r, Config.RoomW - r);
        next.Y = Math.Clamp(next.Y, r, Config.RoomH - r);
        return next;
    }

    // -------------------------------------------------------------- snapshot

    public Snapshot BuildSnapshot()
    {
        var s = new Snapshot
        {
            Tick = Tick,
            Floor = (byte)Floor.Number,
            Score = Score,
            GameOver = GameOver,
            RoomKind = Room.Kind,
            DoorsOpen = Room.Cleared,
            Message = Message,
        };
        for (int d = 0; d < 4; d++)
            if (Room.Doors[d])
                s.DoorMask |= (byte)(1 << d);
        for (int x = 0; x < Config.RoomTilesW; x++)
        for (int y = 0; y < Config.RoomTilesH; y++)
            if (Room.Rocks[x, y])
                s.Rocks.Add((byte)(x + y * Config.RoomTilesW));

        foreach (var p in Players.Values)
        {
            s.Players.Add(
                new PlayerView
                {
                    Id = (byte)p.Id,
                    X = (short)p.Pos.X,
                    Y = (short)p.Pos.Y,
                    LookX = (sbyte)MathF.Round(p.LookDir.X),
                    LookY = (sbyte)MathF.Round(p.LookDir.Y),
                    Health = (byte)p.Health,
                    MaxHealth = (byte)p.Stats.MaxHealth,
                    Blink = p.Invuln > 0,
                    // Rounded so the JSON doesn't carry digits the HUD never shows.
                    Damage = MathF.Round(p.Stats.Damage, 2),
                    Speed = MathF.Round(p.Stats.Speed),
                    TearsPerSecond = MathF.Round(1f / p.Stats.FireDelay, 2),
                    ItemCount = (byte)p.Items.Count,
                    Character = p.Character.Name,
                }
            );
        }
        foreach (var e in Enemies)
        {
            s.Enemies.Add(
                new EnemyView
                {
                    Kind = e.Kind,
                    X = (short)e.Pos.X,
                    Y = (short)e.Pos.Y,
                    Radius = (byte)e.Radius,
                    HpPercent = (byte)Math.Clamp(e.Hp / e.MaxHp * 100, 0, 100),
                    Flash = e.HitFlash > 0,
                }
            );
        }
        foreach (var t in Tears)
            s.Tears.Add(
                new TearView
                {
                    X = (short)t.Pos.X,
                    Y = (short)t.Pos.Y,
                    Radius = (byte)t.Radius,
                    FromEnemy = t.FromEnemy,
                }
            );
        foreach (var pk in Room.Pickups)
            s.Pickups.Add(
                new PickupView
                {
                    Kind = pk.Kind,
                    X = (short)pk.Pos.X,
                    Y = (short)pk.Pos.Y,
                    Item = pk.Item,
                }
            );

        // Minimap: visited rooms plus their direct neighbours (as unexplored).
        foreach (var r in Floor.Rooms.Values)
        {
            bool known =
                r.Visited
                || Enum.GetValues<Direction>().Any(d => Floor.Neighbor(r, d)?.Visited == true);
            if (!known)
                continue;
            byte flags = 0;
            if (r.Visited)
                flags |= MapCell.FlagVisited;
            if (r == Room)
                flags |= MapCell.FlagCurrent;
            s.Map.Add(
                new MapCell
                {
                    X = (sbyte)r.GridX,
                    Y = (sbyte)r.GridY,
                    Kind = r.Kind,
                    Flags = flags,
                }
            );
        }
        return s;
    }
}
