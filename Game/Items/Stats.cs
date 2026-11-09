using Isaac.Core;

namespace Isaac.Game.Items;

/// <summary>
/// DECORATOR pattern, the Component: the stats a player has. The game only ever reads them
/// through this interface, so it cannot tell plain stats from stats wrapped in item decorators.
/// PROTOTYPE pattern: every layer can be cloned, so a <see cref="Characters.Character"/> can
/// deep-copy its whole stats chain through <see cref="IPrototype{T}.DeepClone"/>.
/// </summary>
public interface IStats : IPrototype<IStats>
{
    float Speed { get; }      // pixels per second
    float Damage { get; }
    float FireDelay { get; }  // seconds between tears
    float ShotSpeed { get; }  // pixels per second
    float Range { get; }      // tear lifetime in seconds
    float TearSize { get; }   // radius in pixels
    int MaxHealth { get; }    // in half hearts
}

/// <summary>
/// DECORATOR pattern, the Concrete Component: a character's stats before any items. Each
/// character sets its own values (e.g. <c>new BaseStats { Speed = 220f }</c>); items never change
/// them afterwards, they wrap them in a decorator instead.
/// </summary>
public sealed class BaseStats : IStats
{
    public float Speed { get; init; } = 260f;
    public float Damage { get; init; } = 3.5f;
    public float FireDelay { get; init; } = 0.38f;
    public float ShotSpeed { get; init; } = 520f;
    public float Range { get; init; } = 0.75f;
    public float TearSize { get; init; } = 8f;
    public int MaxHealth { get; init; } = 6;

    public IStats ShallowClone() => (IStats)MemberwiseClone();

    // Every value is a float or an int, so the shallow copy already shares nothing.
    public IStats DeepClone() => ShallowClone();
}

/// <summary>
/// DECORATOR pattern, the Decorator: wraps another <see cref="IStats"/> and passes every stat
/// through unchanged. Concrete decorators override only the stats their item changes, so
/// items stack by wrapping: each one changes the result of everything picked up before it.
/// </summary>
public abstract class StatsDecorator(IStats inner) : IStats
{
    protected IStats Inner { get; private set; } = inner;

    public virtual float Speed => Inner.Speed;
    public virtual float Damage => Inner.Damage;
    public virtual float FireDelay => Inner.FireDelay;
    public virtual float ShotSpeed => Inner.ShotSpeed;
    public virtual float Range => Inner.Range;
    public virtual float TearSize => Inner.TearSize;
    public virtual int MaxHealth => Inner.MaxHealth;

    /// <summary>Copies this layer only: the copy wraps the very same inner stats.</summary>
    public IStats ShallowClone() => (IStats)MemberwiseClone();

    /// <summary>Copies this layer and every layer under it, down to the base stats.</summary>
    public IStats DeepClone()
    {
        var copy = (StatsDecorator)MemberwiseClone();
        copy.Inner = Inner.DeepClone();
        return copy;
    }
}
