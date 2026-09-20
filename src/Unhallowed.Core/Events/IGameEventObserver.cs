namespace Unhallowed.Core.Events;

// PATTERN: Observer -> docs/patterns/19-observer.md
// Deliberately modelled with explicit Subject/Observer interfaces rather than the C# `event`
// keyword, so the structure maps one-to-one onto the GoF class diagram in MagicDraw.

/// <summary>Anything that wants to react to gameplay events implements this.</summary>
public interface IGameEventObserver
{
    void OnNotify(GameEvent gameEvent);
}

/// <summary>The subject half of Observer: keeps observers and broadcasts to them.</summary>
public interface IGameEventSubject
{
    void Attach(IGameEventObserver observer);

    void Detach(IGameEventObserver observer);

    void Notify(GameEvent gameEvent);
}
