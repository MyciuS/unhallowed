using Unhallowed.Core.Common;

namespace Unhallowed.Core.Entities;

/// <summary>A tear / bullet travelling across the room until it expires or hits something.</summary>
public sealed class Projectile : Entity
{
    private float _remainingLife;

    public Projectile(int id, Vec2 position, Vec2 velocity, int damage, int ownerId, float lifeSeconds = 1.6f)
        : base(id, position, 5f)
    {
        Velocity = velocity;
        Damage = damage;
        OwnerId = ownerId;
        _remainingLife = lifeSeconds;
        Health = 1;
        MaxHealth = 1;
    }

    public int Damage { get; }

    /// <summary>Entity that fired this, so it cannot hit its own shooter.</summary>
    public int OwnerId { get; }

    public override string Kind => "projectile";

    public override void Update(float deltaSeconds, IWorldView world)
    {
        Position += Velocity * deltaSeconds;
        _remainingLife -= deltaSeconds;

        var outOfBounds =
            Position.X < 0 || Position.Y < 0 ||
            Position.X > world.RoomSize.X || Position.Y > world.RoomSize.Y;

        if (_remainingLife <= 0f || outOfBounds)
        {
            TakeDamage(1);
        }
    }
}
