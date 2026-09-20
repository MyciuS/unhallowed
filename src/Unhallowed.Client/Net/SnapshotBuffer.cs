using Unhallowed.Contracts.Messages;

namespace Unhallowed.Client.Net;

/// <summary>
/// Holds the last few authoritative snapshots and renders the world slightly in the past,
/// interpolating between the two that bracket the render time. This is what stops remote
/// players teleporting between the 15 snapshots per second the server actually sends.
/// </summary>
internal sealed class SnapshotBuffer
{
    private readonly LinkedList<(DateTime Received, WorldSnapshotDto Snapshot)> _buffer = new();
    private readonly Lock _gate = new();
    private readonly TimeSpan _renderDelay = TimeSpan.FromMilliseconds(100);

    public string RoomId { get; private set; } = string.Empty;

    public int LatestTick { get; private set; }

    public void Push(WorldSnapshotDto snapshot)
    {
        lock (_gate)
        {
            _buffer.AddLast((DateTime.UtcNow, snapshot));
            RoomId = snapshot.RoomId;
            LatestTick = snapshot.Tick;

            while (_buffer.Count > 12)
            {
                _buffer.RemoveFirst();
            }
        }
    }

    /// <summary>Entities as they should be drawn right now, interpolated where possible.</summary>
    public IReadOnlyList<EntitySnapshotDto> GetInterpolated()
    {
        lock (_gate)
        {
            if (_buffer.Count == 0)
            {
                return [];
            }

            if (_buffer.Count == 1)
            {
                return _buffer.Last!.Value.Snapshot.Entities;
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

    private static List<EntitySnapshotDto> Interpolate(
        WorldSnapshotDto from, WorldSnapshotDto to, float t)
    {
        var previousById = from.Entities.ToDictionary(e => e.Id);
        var result = new List<EntitySnapshotDto>(to.Entities.Count);

        foreach (var current in to.Entities)
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
                // Spawned between snapshots: pop it in at its real position.
                result.Add(current);
            }
        }

        return result;
    }
}
