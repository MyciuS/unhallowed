namespace Isaac.Game.Items;

// DECORATOR pattern, the Concrete Decorators: one per item. Each changes only its own stats
// and builds on whatever the wrapped stats say, so the order items were picked up in matters.

public sealed class DamageUpDecorator(IStats inner) : StatsDecorator(inner)
{
    public override float Damage => Inner.Damage + 1.5f;
    public override float TearSize => Inner.TearSize + 1.5f;
}

public sealed class TearsUpDecorator(IStats inner) : StatsDecorator(inner)
{
    public override float FireDelay => MathF.Max(0.08f, Inner.FireDelay * 0.75f);
}

public sealed class SpeedUpDecorator(IStats inner) : StatsDecorator(inner)
{
    public override float Speed => MathF.Min(520f, Inner.Speed + 50f);
}

public sealed class RangeUpDecorator(IStats inner) : StatsDecorator(inner)
{
    public override float Range => Inner.Range + 0.3f;
    public override float ShotSpeed => Inner.ShotSpeed + 60f;
}

public sealed class HeartContainerDecorator(IStats inner) : StatsDecorator(inner)
{
    public override int MaxHealth => Math.Min(24, Inner.MaxHealth + 2);
}

/// <summary>Big damage boost at the cost of a heart container. Doubles all the damage picked up before it.</summary>
public sealed class GlassCannonDecorator(IStats inner) : StatsDecorator(inner)
{
    public override float Damage => Inner.Damage * 2f;
    public override float TearSize => Inner.TearSize + 4f;
    public override int MaxHealth => Math.Max(2, Inner.MaxHealth - 2);
}
