using Unhallowed.Core.Commands;
using Unhallowed.Core.Common;
using Unhallowed.Core.Entities;
using Unhallowed.Core.Events;
using Unhallowed.Core.Rooms;

namespace Unhallowed.Core.Simulation;

/// <summary>
/// The authoritative game world. Exactly one of these exists per match, it lives on the server,
/// and it is only ever touched from the simulation thread.
/// Also the <b>originator</b> of the Memento pattern - see <see cref="Capture"/>.
/// </summary>
public sealed class World : IWorldView
{
    private readonly List<Entity> _entities = [];
    private readonly List<Entity> _spawnBuffer = [];
    private readonly Dictionary<int, Player> _players = [];
    private readonly Dictionary<int, CommandQueue> _inboxes = [];
    private int _nextEntityId = 1;

    public World(RoomBase room, EventBus events)
    {
        CurrentRoom = room ?? throw new ArgumentNullException(nameof(room));
        Events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public RoomBase CurrentRoom { get; private set; }

    public int Tick { get; private set; }

    public IReadOnlyList<Entity> Entities => _entities;

    public IReadOnlyCollection<Player> Players => _players.Values;

    public Vec2 RoomSize => CurrentRoom.Size;

    public IGameEventSubject Events { get; }

    public int NextEntityId() => _nextEntityId++;

    public Player AddPlayer(string displayName, int seat)
    {
        var spawn = new Vec2(RoomSize.X / 2f, RoomSize.Y / 2f) + new Vec2(seat * 28f, 0f);
        var player = new Player(NextEntityId(), displayName, seat, spawn) { World = this };

        _players[player.Id] = player;
        _inboxes[player.Id] = new CommandQueue();
        _entities.Add(player);
        return player;
    }

    public void RemovePlayer(int playerId)
    {
        if (_players.Remove(playerId, out var player))
        {
            _entities.Remove(player);
            _inboxes.Remove(playerId);
        }
    }

    public Player? GetPlayer(int playerId) => _players.GetValueOrDefault(playerId);

    /// <summary>Thread-safe entry point used by the SignalR hub.</summary>
    public void EnqueueCommand(int playerId, IGameCommand command)
    {
        if (_inboxes.TryGetValue(playerId, out var inbox))
        {
            inbox.Enqueue(command);
        }
    }

    public int AckSequenceFor(int playerId)
        => _inboxes.TryGetValue(playerId, out var inbox) ? inbox.LastAcceptedSequence : 0;

    public void EnterRoom(RoomBase room)
    {
        CurrentRoom = room;

        // Keep the players, drop everything else from the previous room.
        _entities.RemoveAll(e => e is not Player);
        room.Enter(this);

        // Populate() spawns through the buffer, so flush here: callers must see a fully
        // populated world the moment EnterRoom returns, not one tick later.
        FlushSpawns();
    }

    /// <summary>Runs exactly one fixed timestep of the authoritative simulation.</summary>
    public void Step(float deltaSeconds)
    {
        Tick++;

        ApplyQueuedCommands();

        foreach (var entity in _entities.ToArray())
        {
            entity.Update(deltaSeconds, this);
        }

        ResolveCollisions();
        FlushSpawns();
        RemoveDeadEntities();
        CurrentRoom.Tick(this);
    }

    private void ApplyQueuedCommands()
    {
        foreach (var (playerId, inbox) in _inboxes)
        {
            if (!_players.TryGetValue(playerId, out var player))
            {
                continue;
            }

            foreach (var command in inbox.Drain())
            {
                command.Execute(player, this);
            }
        }
    }

    private void ResolveCollisions()
    {
        foreach (var projectile in _entities.OfType<Projectile>().Where(p => p.IsAlive))
        {
            foreach (var enemy in _entities.OfType<Enemy>().Where(e => e.IsAlive))
            {
                if (Overlaps(projectile, enemy))
                {
                    enemy.TakeDamage(projectile.Damage);
                    projectile.TakeDamage(1);
                    break;
                }
            }
        }

        foreach (var enemy in _entities.OfType<Enemy>().Where(e => e.IsAlive))
        {
            foreach (var player in _players.Values.Where(p => p.IsAlive))
            {
                if (Overlaps(enemy, player))
                {
                    player.TakeDamage(enemy.ContactDamage);
                }
            }
        }
    }

    private static bool Overlaps(Entity a, Entity b)
        => Vec2.Distance(a.Position, b.Position) <= a.Radius + b.Radius;

    private void FlushSpawns()
    {
        if (_spawnBuffer.Count == 0)
        {
            return;
        }

        _entities.AddRange(_spawnBuffer);
        _spawnBuffer.Clear();
    }

    private void RemoveDeadEntities()
    {
        foreach (var enemy in _entities.OfType<Enemy>().Where(e => e.Died))
        {
            Events.Notify(enemy.BuildDeathEvent());
        }

        // Players stay in the list while dead so co-op revives remain possible.
        _entities.RemoveAll(e => !e.IsAlive && e is not Player);
    }

    public Entity? FindNearestPlayer(Vec2 origin)
    {
        Entity? best = null;
        var bestDistance = float.MaxValue;

        foreach (var player in _players.Values.Where(p => p.IsAlive))
        {
            var distance = Vec2.Distance(origin, player.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = player;
            }
        }

        return best;
    }

    /// <summary>Buffered until the end of the tick so the update loop is not mutated mid-iteration.</summary>
    public void Spawn(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _spawnBuffer.Add(entity);
    }

    /// <summary>Produces an immutable memento of the world as it stands right now.</summary>
    public WorldSnapshot Capture() => new(
        Tick,
        CurrentRoom.Id,
        [.. _entities.Select(e => new EntityMemento(
            e.Id, e.Kind, e.Visual, e.Position, e.Radius, e.Health, e.MaxHealth))]);

    /// <summary>
    /// Restores entity transforms and health from a memento. Used for lag compensation:
    /// rewind, re-test a hit, roll forward again.
    /// </summary>
    public void Restore(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var byId = _entities.ToDictionary(e => e.Id);
        foreach (var memento in snapshot.Entities)
        {
            if (byId.TryGetValue(memento.Id, out var entity))
            {
                entity.Position = memento.Position;
            }
        }

        Tick = snapshot.Tick;
    }
}
