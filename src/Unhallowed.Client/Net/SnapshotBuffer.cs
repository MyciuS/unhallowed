using Unhallowed.Contracts.Messages;

namespace Unhallowed.Client.Net;

/// <summary>
/// Holds the last few authoritative snapshots and renders the world slightly in the past,
/// interpolating between the two that bracket the render time. This is what stops remote
/// players teleporting between the snapshots the server actually sends.
/// </summary>
internal sealed class SnapshotBuffer
{
    private readonly LinkedList<(DateTime Received, WorldSnapshotDto Snapshot)> _buffer = new();
    private readonly Lock _gate = new();
    private readonly TimeSpan _renderDelay = TimeSpan.FromMilliseconds(100);

    public int LatestTick { get; private set; }

    public void Push(WorldSnapshotDto snapshot)
    {
        lock (_gate)
        {
            _buffer.AddLast((DateTime.UtcNow, snapshot));
            LatestTick = snapshot.Tick;

            while (_buffer.Count > 12)
            {
                _buffer.RemoveFirst();
            }
        }
    }

    /// <summary>Players as they should be drawn right now, interpolated where possible.</summary>
    public IReadOnlyList<PlayerSnapshotDto> GetInterpolated()
    {
        lock (_gate)
        {
            if (_buffer.Count == 0)
            {
                return [];
            }

            if (_buffer.Count == 1)
            {
                return _buffer.Last!.Value.Snapshot.Players;
            }

            var renderTime = DateTime.UtcNow - _renderDelay;

            var node = _buffer.Last;
            while (node?.Previous is not null && node.Value.Received > renderTime)
            {
                node = node.Previous;
            }

            var older = node!.Value;
            var newer = node.Next?.Value ?? older;

            var span = (newer.Received - older.Received).TotalSeconds;
            var t = span <= 0
                ? 1f
                : (float)Math.Clamp((renderTime - older.Received).TotalSeconds / span, 0d, 1d);

            return Interpolate(older.Snapshot, newer.Snapshot, t);
        }
    }

    private static List<PlayerSnapshotDto> Interpolate(WorldSnapshotDto from, WorldSnapshotDto to, float t)
    {
        var previousById = from.Players.ToDictionary(p => p.Id);
        var result = new List<PlayerSnapshotDto>(to.Players.Count);

        foreach (var current in to.Players)
        {
            if (previousById.TryGetValue(current.Id, out var previous))
            {
                result.Add(current with
                {
                    X = previous.X + ((current.X - previous.X) * t),
                    Y = previous.Y + ((current.Y - previous.Y) * t),
                });
            }
            else
            {
                // Joined between snapshots: pop them in at their real position.
                result.Add(current);
            }
        }

        return result;
    }
}
