namespace Isaac.Core;

/// <summary>
/// OBSERVER pattern, the Subject: a type-safe publish/subscribe hub. Game systems
/// publish events (enemy killed, room cleared, ...) without knowing who listens;
/// listeners register through <see cref="IEventListener{T}"/>.
/// </summary>
public sealed class EventBus
{
    private readonly Dictionary<Type, List<object>> _listeners = new();

    public void Subscribe<T>(IEventListener<T> listener)
    {
        if (!_listeners.TryGetValue(typeof(T), out var list))
            _listeners[typeof(T)] = list = new List<object>();
        if (!list.Contains(listener))
            list.Add(listener);
    }

    public void Unsubscribe<T>(IEventListener<T> listener)
    {
        if (_listeners.TryGetValue(typeof(T), out var list))
            list.Remove(listener);
    }

    public void Publish<T>(T evt)
    {
        if (!_listeners.TryGetValue(typeof(T), out var list))
            return;
        // Copy so listeners may (un)subscribe while being notified.
        foreach (var listener in list.ToArray())
            ((IEventListener<T>)listener).OnEvent(evt);
    }
}
