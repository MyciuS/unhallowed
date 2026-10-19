namespace Isaac.Game.Items;

/// <summary>
/// DECORATOR pattern, the Component: the stats a player has. The game only ever reads them
/// through this interface, so it cannot tell plain stats from stats wrapped in item decorators.
/// </summary>
public interface IStats
{
    float Speed { get; }      // pixels per second
    float Damage { get; }
    float FireDelay { get; }  // seconds between tears
    float ShotSpeed { get; }  // pixels per second
    float Range { get; }      // tear lifetime in seconds
    float TearSize { get; }   // radius in pixels
    int MaxHealth { get; }    // in half hearts
}

/// <summary>DECORATOR pattern, the Concrete Component: a fresh character's stats, before any items.</summary>
public sealed class BaseStats : IStats
{
    public float Speed => 260f;
    public float Damage => 3.5f;
    public float FireDelay => 0.38f;
    public float ShotSpeed => 520f;
    public float Range => 0.75f;
    public float TearSize => 8f;
    public int MaxHealth => 6;
}

/// <summary>
/// DECORATOR pattern, the Decorator: wraps another <see cref="IStats"/> and passes every stat
/// through unchanged. Concrete decorators override only the stats their item changes, so
/// items stack by wrapping: each one changes the result of everything picked up before it.
/// </summary>
public abstract class StatsDecorator(IStats inner) : IStats
{
    protected IStats Inner { get; } = inner;

    public virtual float Speed => Inner.Speed;
    public virtual float Damage => Inner.Damage;
    public virtual float FireDelay => Inner.FireDelay;
    public virtual float ShotSpeed => Inner.ShotSpeed;
    public virtual float Range => Inner.Range;
    public virtual float TearSize => Inner.TearSize;
    public virtual int MaxHealth => Inner.MaxHealth;
}
