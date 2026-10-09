namespace Isaac.Game.Items;

/// <summary>Player stats. Picking up an item changes them directly.</summary>
public sealed class Stats
{
    public float Speed = 260f;      // pixels per second
    public float Damage = 3.5f;
    public float FireDelay = 0.38f; // seconds between tears
    public float ShotSpeed = 520f;  // pixels per second
    public float Range = 0.75f;     // tear lifetime in seconds
    public float TearSize = 8f;     // radius in pixels
    public int MaxHealth = 6;       // in half hearts

    public void Apply(ItemKind item)
    {
        switch (item)
        {
            case ItemKind.DamageUp:
                Damage += 1.5f;
                TearSize += 1.5f;
                break;
            case ItemKind.TearsUp:
                FireDelay = MathF.Max(0.08f, FireDelay * 0.75f);
                break;
            case ItemKind.SpeedUp:
                Speed = MathF.Min(520f, Speed + 50f);
                break;
            case ItemKind.RangeUp:
                Range += 0.3f;
                ShotSpeed += 60f;
                break;
            case ItemKind.HeartContainer:
                MaxHealth = Math.Min(24, MaxHealth + 2);
                break;
            case ItemKind.GlassCannon:
                // Big damage boost at the cost of a heart container.
                Damage *= 2f;
                TearSize += 4f;
                MaxHealth = Math.Max(2, MaxHealth - 2);
                break;
        }
    }
}
