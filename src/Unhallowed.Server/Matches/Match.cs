using Unhallowed.Contracts;
using Unhallowed.Contracts.Messages;
using Unhallowed.Core.Common;
using Unhallowed.Core.Events;
using Unhallowed.Core.Rooms;
using Unhallowed.Core.Simulation;

namespace Unhallowed.Server.Matches;

/// <summary>
/// One lobby: an authoritative <see cref="World"/> plus the connections watching it.
/// Everything gameplay-related happens here and never on a client.
/// </summary>
public sealed class Match
{
    private readonly Dictionary<string, int> _connectionToPlayer = [];
    private readonly Lock _gate = new();
    private int _nextSeat;

    public Match(string code)
    {
        Code = code;
        Events = new EventBus();
        History = new SnapshotHistory();

        var size = new Vec2(960f, 540f);
        var firstRoom = new NormalRoom($"{code}-r1", size, code.GetHashCode());
        World = new World(firstRoom, Events);
        World.EnterRoom(firstRoom);
    }

    public string Code { get; }

    public World World { get; }

    public EventBus Events { get; }

    public SnapshotHistory History { get; }

    public bool IsEmpty
    {
        get
        {
            lock (_gate)
            {
                return _connectionToPlayer.Count == 0;
            }
        }
    }

    public bool IsFull
    {
        get
        {
            lock (_gate)
            {
                return _connectionToPlayer.Count >= Protocol.MaxPlayers;
            }
        }
    }

    public IReadOnlyDictionary<string, int> Connections
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<string, int>(_connectionToPlayer);
            }
        }
    }

    public MatchJoinedResponse Join(string connectionId, string displayName)
    {
        lock (_gate)
        {
            var seat = _nextSeat++ % Protocol.MaxPlayers;
            var player = World.AddPlayer(displayName, seat);
            _connectionToPlayer[connectionId] = player.Id;
            return new MatchJoinedResponse(Code, player.Id, seat, World.CurrentRoom.Id);
        }
    }

    public int? Leave(string connectionId)
    {
        lock (_gate)
        {
            if (!_connectionToPlayer.Remove(connectionId, out var playerId))
            {
                return null;
            }

            World.RemovePlayer(playerId);
            return playerId;
        }
    }

    public int? PlayerIdFor(string connectionId)
    {
        lock (_gate)
        {
            return _connectionToPlayer.TryGetValue(connectionId, out var id) ? id : null;
        }
    }
}
