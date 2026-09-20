using System.Collections.Concurrent;
using Unhallowed.Core.Common;
using Unhallowed.Core.Entities;

namespace Unhallowed.Core.Simulation;

/// <summary>
/// The authoritative game world. Exactly one exists per match and it lives on the server.
/// Players are kept in a concurrent map: the SignalR hub writes move intents from connection
/// threads while the fixed-step loop reads and advances them from the loop thread.
/// </summary>
public sealed class World
{
    /// <summary>Player walk speed in world units per second.</summary>
    public const float MoveSpeed = 200f;

    private readonly ConcurrentDictionary<int, Player> _players = new();
    private readonly Vec2 _arenaSize;
    private readonly float _playerRadius;
    private int _nextId;

    public World(Vec2 arenaSize, float playerRadius)
    {
        _arenaSize = arenaSize;
        _playerRadius = playerRadius;
    }

    public int Tick { get; private set; }

    public IReadOnlyCollection<Player> Players => _players.Values.ToArray();

    public Player AddPlayer(string displayName, int seat)
    {
        var id = Interlocked.Increment(ref _nextId);
        var spawn = new Vec2((_arenaSize.X / 2f) + (seat * 28f), _arenaSize.Y / 2f);
        var player = new Player(id, displayName, seat, spawn);
        _players[id] = player;
        return player;
    }

    public void RemovePlayer(int playerId) => _players.TryRemove(playerId, out _);

    /// <summary>Thread-safe entry point used by the SignalR hub.</summary>
    public void SetMoveIntent(int playerId, Vec2 intent)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            player.MoveIntent = intent;
        }
    }

    /// <summary>Runs exactly one fixed timestep of the authoritative simulation.</summary>
    public void Step(float deltaSeconds)
    {
        Tick++;

        foreach (var player in _players.Values)
        {
            player.Update(deltaSeconds, MoveSpeed, _arenaSize, _playerRadius);
        }
    }
}
