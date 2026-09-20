using Unhallowed.Core.Common;
using Unhallowed.Core.Entities.Stats;
using Unhallowed.Core.Events;
using Unhallowed.Core.States;

namespace Unhallowed.Core.Entities;

/// <summary>
/// A player character. Note what this class does <i>not</i> contain: no movement-mode switch
/// (see <see cref="IPlayerState"/>) and no item-effect arithmetic (see <see cref="IPlayerStats"/>).
/// </summary>
public sealed class Player : Entity
{
    private IPlayerState _state = new IdleState();
    private float _fireCooldown;

    public Player(int id, string displayName, int seat, Vec2 position)
        : base(id, position, 12f)
    {
        DisplayName = displayName;
        Seat = seat;
        Stats = new BaseStats();
        MaxHealth = Stats.MaxHealth;
        Health = MaxHealth;
    }

    public string DisplayName { get; }

    /// <summary>0-3, decides the player colour drawn by the GDI+ renderer.</summary>
    public int Seat { get; }

    // PATTERN: Decorator - this reference is the head of the item chain.
    public IPlayerStats Stats { get; private set; }

    /// <summary>Direction the player wants to walk, set from the latest Move command.</summary>
    public Vec2 MoveIntent { get; set; }

    /// <summary>Direction the player wants to shoot, set from the latest Shoot command.</summary>
    public Vec2 AimIntent { get; set; }

    public string StateName => _state.Name;

    /// <summary>Set when the player is added to a world; lets states raise events.</summary>
    public IWorldView? World { get; internal set; }

    public override string Kind => "player";

    public override string Visual => $"player{Seat}";

    /// <summary>
    /// Wraps the current stat chain in one more decorator. Picking up ten items produces a
    /// ten-link chain and no change at all to this class.
    /// </summary>
    public void PickUp(Func<IPlayerStats, StatDecorator> itemFactory)
    {
        var decorated = itemFactory(Stats);
        Stats = decorated;

        // Extra heart containers should actually grant the hearts.
        var newMax = Stats.MaxHealth;
        if (newMax > MaxHealth)
        {
            var gained = newMax - MaxHealth;
            MaxHealth = newMax;
            Health += gained;
        }

        World?.Events.Notify(new GameEvent(
            GameEventType.ItemPickedUp, Id, $"{DisplayName} picked up {decorated.ItemName}"));
    }

    public override void Update(float deltaSeconds, IWorldView world)
    {
        World = world;

        var next = _state.Update(this, deltaSeconds, world);
        if (!ReferenceEquals(next, _state))
        {
            _state.Exit(this);
            _state = next;
            _state.Enter(this);
        }

        if (!_state.AcceptsInput)
        {
            Velocity = Vec2.Zero;
        }

        Position += Velocity * deltaSeconds;
        ClampToRoom(world.RoomSize);

        _fireCooldown -= deltaSeconds;
        if (AimIntent.Length > 0.01f && _fireCooldown <= 0f && IsAlive)
        {
            Shoot(world);
            _fireCooldown = 1f / Math.Max(0.1f, Stats.FireRate);
        }
    }

    public override void TakeDamage(int amount)
    {
        if (_state.IsInvulnerable)
        {
            return;
        }

        base.TakeDamage(amount);
        World?.Events.Notify(new GameEvent(
            GameEventType.PlayerDamaged, Id, $"{DisplayName} took {amount} damage"));

        if (IsAlive)
        {
            _state.Exit(this);
            _state = new HurtState();
            _state.Enter(this);
        }
    }

    private void Shoot(IWorldView world)
    {
        var direction = AimIntent.Normalized();
        var projectile = new Projectile(
            world.NextEntityId(),
            Position + (direction * (Radius + 6f)),
            direction * 320f,
            (int)MathF.Round(Stats.Damage),
            Id);

        world.Spawn(projectile);
    }

    private void ClampToRoom(Vec2 roomSize)
    {
        Position = new Vec2(
            Math.Clamp(Position.X, Radius, roomSize.X - Radius),
            Math.Clamp(Position.Y, Radius, roomSize.Y - Radius));
    }
}
