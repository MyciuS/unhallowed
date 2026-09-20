using Unhallowed.Core.Common;

namespace Unhallowed.Core.Simulation;

// PATTERN: Memento -> docs/patterns/18-memento.md
// The snapshot is the memento: an opaque, immutable record of world state at one tick.
// World is the originator (it alone can create and restore one), SnapshotHistory is the
// caretaker (it stores them without ever inspecting their contents), and the server also
// serialises the newest one to JSON for the clients.

/// <summary>One entity's state inside a memento.</summary>
public sealed record EntityMemento(
    int Id,
    string Kind,
    string Visual,
    Vec2 Position,
    float Radius,
    int Health,
    int MaxHealth);

/// <summary>An immutable capture of the entire world at a single tick.</summary>
public sealed record WorldSnapshot(
    int Tick,
    string RoomId,
    IReadOnlyList<EntityMemento> Entities);

/// <summary>
/// Caretaker. Keeps a bounded rolling history of mementos so the server can roll back for
/// lag compensation or dump the last seconds of a match when something goes wrong.
/// </summary>
public sealed class SnapshotHistory(int capacity = 64)
{
    private readonly Queue<WorldSnapshot> _history = new();

    public int Count => _history.Count;

    public void Push(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _history.Enqueue(snapshot);
        while (_history.Count > capacity)
        {
            _history.Dequeue();
        }
    }

    /// <summary>Most recent memento, or null when nothing has been captured yet.</summary>
    public WorldSnapshot? Latest => _history.Count == 0 ? null : _history.Last();

    /// <summary>Finds the memento captured at <paramref name="tick"/>, if it is still held.</summary>
    public WorldSnapshot? FindByTick(int tick) => _history.FirstOrDefault(s => s.Tick == tick);

    public void Clear() => _history.Clear();
}
