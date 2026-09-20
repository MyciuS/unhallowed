namespace Unhallowed.Core.Events;

// PATTERN: Observer (concrete subject) -> docs/patterns/19-observer.md

/// <summary>
/// Concrete subject. The simulation publishes here; HUD, audio, scoring and the network
/// broadcaster subscribe. Publishers never learn who is listening.
/// </summary>
public sealed class EventBus : IGameEventSubject
{
    private readonly List<IGameEventObserver> _observers = [];
    private readonly Lock _gate = new();

    public void Attach(IGameEventObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        lock (_gate)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }
    }

    public void Detach(IGameEventObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        lock (_gate)
        {
            _observers.Remove(observer);
        }
    }

    public void Notify(GameEvent gameEvent)
    {
        ArgumentNullException.ThrowIfNull(gameEvent);

        IGameEventObserver[] snapshot;
        lock (_gate)
        {
            // Copy first: an observer is allowed to detach itself while being notified.
            snapshot = [.. _observers];
        }

        foreach (var observer in snapshot)
        {
            observer.OnNotify(gameEvent);
        }
    }
}
