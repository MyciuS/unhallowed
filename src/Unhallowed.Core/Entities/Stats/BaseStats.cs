namespace Unhallowed.Core.Entities.Stats;

// PATTERN: Decorator (concrete component) -> docs/patterns/09-decorator.md

/// <summary>An unmodified character with no items. Always the innermost link of the chain.</summary>
public sealed class BaseStats : IPlayerStats
{
    public float Damage => 3.5f;

    public float MoveSpeed => 140f;

    public float FireRate => 2.2f;

    public int MaxHealth => 6;

    public string Describe() => "Base";
}
