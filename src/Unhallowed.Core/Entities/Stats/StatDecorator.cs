namespace Unhallowed.Core.Entities.Stats;

// PATTERN: Decorator (abstract decorator) -> docs/patterns/09-decorator.md

/// <summary>
/// Base for every item that modifies stats. Forwards everything to the wrapped component by
/// default, so a concrete item only overrides the one or two stats it actually changes.
/// </summary>
public abstract class StatDecorator : IPlayerStats
{
    protected StatDecorator(IPlayerStats inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    protected IPlayerStats Inner { get; }

    /// <summary>Item name as it appears on the pickup banner.</summary>
    public abstract string ItemName { get; }

    public virtual float Damage => Inner.Damage;

    public virtual float MoveSpeed => Inner.MoveSpeed;

    public virtual float FireRate => Inner.FireRate;

    public virtual int MaxHealth => Inner.MaxHealth;

    public string Describe() => $"{Inner.Describe()} + {ItemName}";
}
