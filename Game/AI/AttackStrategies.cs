using System.Numerics;
using Isaac.Game.Entities;

namespace Isaac.Game.AI;

/// <summary>
/// STRATEGY pattern, the Strategy interface for attacks: decides if and how an enemy shoots.
/// An <see cref="Enemy"/> runs all of its attack strategies every frame. Each one keeps its own cooldown.
/// </summary>
public interface IAttackStrategy
{
    void Attack(Enemy self, Player? target, World world, float dt);
}

/// <summary>
/// TEMPLATE METHOD pattern, the Abstract Class: an attack that fires on a cooldown.
/// <see cref="Attack"/> is the template method. It fixes the steps every cooldown attack goes
/// through (run the timer down, ask <see cref="CanFire"/>, restart the timer, <see cref="Fire"/>)
/// and is not virtual, so subclasses fill in steps but cannot change their order.
/// </summary>
public abstract class CooldownAttack(float cooldown) : IAttackStrategy
{
    private readonly float _cooldown = cooldown;
    // Random first delay so a room full of shooters does not fire in sync.
    private float _timer = cooldown * (0.5f + Random.Shared.NextSingle());

    /// <summary>Seconds until the attack is ready. At 0 or below it fires as soon as <see cref="CanFire"/> allows.</summary>
    public float TimeLeft => _timer;

    public void Attack(Enemy self, Player? target, World world, float dt)
    {
        _timer -= dt;
        // The timer keeps running out while CanFire says no, so the attack fires the moment it says yes.
        if (_timer > 0 || target == null || !CanFire(self, target, world))
            return;
        _timer = _cooldown;
        Fire(self, target, world);
    }

    /// <summary>Hook: an extra condition for firing once the cooldown is over. By default there is none.</summary>
    protected virtual bool CanFire(Enemy self, Player target, World world) => true;

    /// <summary>Primitive operation: spawns this attack's tears.</summary>
    protected abstract void Fire(Enemy self, Player target, World world);
}

/// <summary>
/// TEMPLATE METHOD pattern, a Concrete Class: shoots a single tear at the nearest player,
/// but only when no rock is in the way and it is not in the air.
/// </summary>
public sealed class AimedShotAttack(float cooldown, float tearSpeed) : CooldownAttack(cooldown)
{
    protected override bool CanFire(Enemy self, Player target, World world) =>
        self.Height <= 0 && CanHit(world, self.Pos, target.Pos);

    protected override void Fire(Enemy self, Player target, World world) =>
        world.SpawnTear(self.Pos, Steering.Toward(self.Pos, target.Pos) * tearSpeed, 1, 8f, 2.5f, fromEnemy: true);

    /// <summary>
    /// True if a tear fired from <paramref name="from"/> would reach <paramref name="to"/>.
    /// Tears only stop when their centre enters a rock, so a clear line of sight is enough.
    /// </summary>
    public static bool CanHit(World world, Vector2 from, Vector2 to) => world.HasLineOfSight(from, to);
}

/// <summary>Shoots a ring of tears in all directions each time the enemy lands from a jump.</summary>
public sealed class LandingRingAttack(int count, float tearSpeed) : IAttackStrategy
{
    private float _rotation;

    public void Attack(Enemy self, Player? target, World world, float dt)
    {
        if (!self.JustLanded)
            return;
        // Alternate the ring's angle so the gaps are not always in the same place.
        _rotation += MathF.PI / count;
        for (int i = 0; i < count; i++)
        {
            float a = _rotation + i * MathF.Tau / count;
            var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
            world.SpawnTear(self.Pos + dir * self.Radius, dir * tearSpeed, 1, 9f, 3f, fromEnemy: true);
        }
    }
}

/// <summary>
/// TEMPLATE METHOD pattern, a Concrete Class: spits a messy cone of tears of different sizes and
/// speeds at the player. It keeps the default <see cref="CooldownAttack.CanFire"/>, so rocks do not stop it.
/// </summary>
public sealed class SpitSprayAttack(int count, float cooldown, float spreadDegrees, float minSpeed, float maxSpeed)
    : CooldownAttack(cooldown)
{
    protected override void Fire(Enemy self, Player target, World world)
    {
        var aim = Steering.Toward(self.Pos, target.Pos);
        float baseAngle = MathF.Atan2(aim.Y, aim.X);
        float spread = spreadDegrees * MathF.PI / 180f;
        for (int i = 0; i < count; i++)
        {
            float a = baseAngle + (Random.Shared.NextSingle() * 2 - 1) * spread;
            var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
            float speed = minSpeed + Random.Shared.NextSingle() * (maxSpeed - minSpeed);
            float size = 6f + Random.Shared.NextSingle() * 5f;
            world.SpawnTear(self.Pos + dir * self.Radius * 0.8f, dir * speed, 1, size, 2.5f, fromEnemy: true);
        }
    }
}
