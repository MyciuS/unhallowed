using System.Numerics;
using Isaac.Game.Characters;
using Isaac.Game.Items;

namespace Isaac.Game.Entities;

public sealed class Player
{
    public const float Radius = 18f;
    public const float InvulnTime = 1.0f;

    /// <param name="character">This player's own clone of a character prototype.</param>
    public Player(int id, Character character)
    {
        Id = id;
        Character = character;
        Health = Stats.MaxHealth;
    }

    public int Id { get; }
    public Character Character { get; }
    public Vector2 Pos;
    public Vector2 Vel;
    public Stats Stats => Character.Stats;
    public List<ItemKind> Items => Character.Items;
    public int Health;               // half hearts
    public float FireCooldown;
    public float Invuln;
    public Vector2 MoveIntent;       // from PlayerInput
    public Vector2 ShootIntent;      // from PlayerInput
    public Vector2 LookDir = new(0, 1);

    public bool Alive => Health > 0;

    public void GiveItem(ItemKind item)
    {
        int oldMax = Stats.MaxHealth;
        Character.AddItem(item);
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
