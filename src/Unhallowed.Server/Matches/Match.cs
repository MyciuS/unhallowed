using Unhallowed.Contracts;
using Unhallowed.Contracts.Messages;
using Unhallowed.Core.Common;
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
        World = new World(new Vec2(Protocol.ArenaWidth, Protocol.ArenaHeight), Protocol.PlayerRadius);
    }

    public string Code { get; }

    public World World { get; }

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

    public MatchJoinedResponse Join(string connectionId, string displayName)
    {
        lock (_gate)
        {
            var seat = _nextSeat++ % Protocol.MaxPlayers;
            var player = World.AddPlayer(displayName, seat);
            _connectionToPlayer[connectionId] = player.Id;
            return new MatchJoinedResponse(Code, player.Id, seat);
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
