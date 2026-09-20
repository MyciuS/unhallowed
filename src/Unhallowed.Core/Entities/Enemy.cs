using Unhallowed.Core.AI;
using Unhallowed.Core.Common;
using Unhallowed.Core.Events;

namespace Unhallowed.Core.Entities;

/// <summary>
/// A hostile entity. Its movement is delegated entirely to an <see cref="IMovementStrategy"/>,
/// so "Chaser" and "Charger" are the same class with different strategies injected.
/// </summary>
public sealed class Enemy : Entity
{
    private readonly IMovementStrategy _movement;

    public Enemy(
        int id,
        string archetype,
        Vec2 position,
        int health,
        int contactDamage,
        IMovementStrategy movement,
        float radius = 14f)
        : base(id, position, radius)
    {
        Archetype = archetype;
        MaxHealth = health;
        Health = health;
        ContactDamage = contactDamage;
        _movement = movement;
    }

    /// <summary>Name of the enemy type, e.g. <c>gaper</c> or <c>charger</c>.</summary>
    public string Archetype { get; }

    public int ContactDamage { get; }

    public string MovementName => _movement.Name;

    public override string Kind => "enemy";

    public override string Visual => Archetype;

    public override void Update(float deltaSeconds, IWorldView world)
    {
        // PATTERN: Strategy - the whole of this enemy's movement logic is one delegated call.
        Velocity = _movement.ComputeVelocity(this, world, deltaSeconds);
        Position += Velocity * deltaSeconds;

        Position = new Vec2(
            Math.Clamp(Position.X, Radius, world.RoomSize.X - Radius),
            Math.Clamp(Position.Y, Radius, world.RoomSize.Y - Radius));
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
        if (!IsAlive)
        {
            Died = true;
        }
    }

    /// <summary>Set once when health reaches zero, so the world raises the event exactly once.</summary>
    public bool Died { get; private set; }

    public GameEvent BuildDeathEvent() =>
        new(GameEventType.EnemyDied, Id, $"{Archetype} died");
}
