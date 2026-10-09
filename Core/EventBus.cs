namespace Isaac.Core;

/// <summary>
/// OBSERVER pattern: a type-safe publish/subscribe hub. Game systems publish
/// events (enemy killed, room cleared, ...) without knowing who listens.
/// </summary>
public sealed class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
            _handlers[typeof(T)] = list = new List<Delegate>();
        list.Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
            list.Remove(handler);
    }

    public void Publish<T>(T evt)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
            return;
        // Copy so handlers may (un)subscribe while being notified.
        foreach (var handler in list.ToArray())
            ((Action<T>)handler)(evt);
    }
}
