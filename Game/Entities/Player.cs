using System.Numerics;
using Isaac.Game.Items;

namespace Isaac.Game.Entities;

public sealed class Player
{
    public const float Radius = 18f;
    public const float InvulnTime = 1.0f;

    public Player(int id)
    {
        Id = id;
        Health = Stats.MaxHealth;
    }

    public int Id { get; }
    public Vector2 Pos;
    public Vector2 Vel;
    public Stats Stats { get; } = new();
    public int Health;               // half hearts
    public float FireCooldown;
    public float Invuln;
    public Vector2 MoveIntent;       // from PlayerInput
    public Vector2 ShootIntent;      // from PlayerInput
    public Vector2 LookDir = new(0, 1);
    public readonly List<ItemKind> Items = new();

    public bool Alive => Health > 0;

    public void GiveItem(ItemKind item)
    {
        int oldMax = Stats.MaxHealth;
        Stats.Apply(item);
        Items.Add(item);
        // Gaining a container also fills it; losing one clamps current health.
        if (Stats.MaxHealth > oldMax) Health += Stats.MaxHealth - oldMax;
        Health = Math.Min(Health, Stats.MaxHealth);
    }

    public bool TryDamage(int halfHearts)
    {
        if (!Alive || Invuln > 0) return false;
        Health = Math.Max(0, Health - halfHearts);
        Invuln = InvulnTime;
        return true;
    }
}
