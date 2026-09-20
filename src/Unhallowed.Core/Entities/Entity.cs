using Unhallowed.Core.Common;

namespace Unhallowed.Core.Entities;

/// <summary>Base for everything that exists in a room and can be sent to clients.</summary>
public abstract class Entity
{
    protected Entity(int id, Vec2 position, float radius)
    {
        Id = id;
        Position = position;
        Radius = radius;
    }

    public int Id { get; }

    public Vec2 Position { get; set; }

    public Vec2 Velocity { get; set; }

    public float Radius { get; }

    public int Health { get; protected set; }

    public int MaxHealth { get; protected set; }

    public bool IsAlive => Health > 0;

    /// <summary>Discriminator written into the snapshot so the client knows what to draw.</summary>
    public abstract string Kind { get; }

    /// <summary>Tells the GDI+ renderer which primitive shape and colour to use.</summary>
    public virtual string Visual => Kind;

    /// <summary>Advance this entity by one fixed timestep.</summary>
    public abstract void Update(float deltaSeconds, IWorldView world);

    public virtual void TakeDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
    }

    public void Heal(int amount)
    {
        Health = Math.Min(MaxHealth, Health + amount);
    }
}
