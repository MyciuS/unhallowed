namespace Unhallowed.Core.Entities.Stats;

// PATTERN: Decorator (concrete decorators) -> docs/patterns/09-decorator.md
// Each item is one wrapper. Stacking is free: wrap twice and the bonus applies twice.

/// <summary>Flat damage bonus. Stacks additively.</summary>
public sealed class DamageUp(IPlayerStats inner, float bonus = 1.5f) : StatDecorator(inner)
{
    public override string ItemName => "Damage Up";

    public override float Damage => Inner.Damage + bonus;
}

/// <summary>Movement bonus. Stacks additively.</summary>
public sealed class SpeedUp(IPlayerStats inner, float bonus = 25f) : StatDecorator(inner)
{
    public override string ItemName => "Speed Up";

    public override float MoveSpeed => Inner.MoveSpeed + bonus;
}

/// <summary>Multiplicative fire-rate bonus, so stacking it has diminishing absolute returns.</summary>
public sealed class RapidFire(IPlayerStats inner, float multiplier = 1.25f) : StatDecorator(inner)
{
    public override string ItemName => "Rapid Fire";

    public override float FireRate => Inner.FireRate * multiplier;
}

/// <summary>Extra heart containers, at the cost of a little speed - a classic Isaac trade-off.</summary>
public sealed class HeartContainer(IPlayerStats inner, int bonus = 2) : StatDecorator(inner)
{
    public override string ItemName => "Heart Container";

    public override int MaxHealth => Inner.MaxHealth + bonus;

    public override float MoveSpeed => Inner.MoveSpeed - 5f;
}
