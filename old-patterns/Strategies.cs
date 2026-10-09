using System.Numerics;
using Isaac.Game.Entities;

namespace Isaac.Game.AI;

/// <summary>STRATEGY pattern: how an enemy moves.</summary>
public interface IMovementStrategy
{
    void Move(Enemy self, World world, float dt);
}

/// <summary>STRATEGY pattern: how (and if) an enemy attacks.</summary>
public interface IAttackStrategy
{
    void Update(Enemy self, World world, float dt);
}

/// <summary>Walk straight at the nearest living player.</summary>
public sealed class ChaseMovement : IMovementStrategy
{
    public void Move(Enemy self, World world, float dt)
    {
        var target = world.NearestPlayer(self.Pos);
        if (target == null) { self.Vel = Vector2.Zero; return; }
        var dir = Vector2.Normalize(target.Pos - self.Pos + new Vector2(0.001f));
        self.Vel = dir * self.Speed;
    }
}

/// <summary>Erratic buzzing towards the player, like Isaac's flies.</summary>
public sealed class BuzzMovement : IMovementStrategy
{
    private float _phase = Random.Shared.NextSingle() * MathF.Tau;

    public void Move(Enemy self, World world, float dt)
    {
        _phase += dt * 6f;
        var target = world.NearestPlayer(self.Pos);
        var toward = target == null ? Vector2.Zero : Vector2.Normalize(target.Pos - self.Pos + new Vector2(0.001f));
        var wobble = new Vector2(MathF.Cos(_phase), MathF.Sin(_phase * 1.3f));
        self.Vel = Vector2.Normalize(toward + wobble * 0.9f + new Vector2(0.001f)) * self.Speed;
    }
}

/// <summary>Keeps a preferred distance from the player, strafing around them.</summary>
public sealed class KeepDistanceMovement(float preferred) : IMovementStrategy
{
    private readonly float _strafeSign = Random.Shared.Next(2) == 0 ? -1 : 1;

    public void Move(Enemy self, World world, float dt)
    {
        var target = world.NearestPlayer(self.Pos);
        if (target == null) { self.Vel = Vector2.Zero; return; }
        var delta = target.Pos - self.Pos;
        float dist = delta.Length();
        var dir = dist > 0.01f ? delta / dist : Vector2.UnitX;
        var strafe = new Vector2(-dir.Y, dir.X) * _strafeSign;
        float radial = dist > preferred + 40 ? 1 : dist < preferred - 40 ? -1 : 0;
        self.Vel = Vector2.Normalize(dir * radial + strafe * 0.6f) * self.Speed;
    }
}

/// <summary>Wanders slowly; when lined up with a player, dashes at them.</summary>
public sealed class ChargeMovement : IMovementStrategy
{
    private Vector2 _dashDir;
    private float _dashTime;
    private float _wanderTime;
    private Vector2 _wanderDir;

    public void Move(Enemy self, World world, float dt)
    {
        if (_dashTime > 0)
        {
            _dashTime -= dt;
            self.Vel = _dashDir * self.Speed * 3.2f;
            return;
        }

        var target = world.NearestPlayer(self.Pos);
        if (target != null)
        {
            var d = target.Pos - self.Pos;
            if (MathF.Abs(d.X) < 24 || MathF.Abs(d.Y) < 24)
            {
                _dashDir = MathF.Abs(d.X) < 24 ? new Vector2(0, MathF.Sign(d.Y)) : new Vector2(MathF.Sign(d.X), 0);
                _dashTime = 0.6f;
                return;
            }
        }

        _wanderTime -= dt;
        if (_wanderTime <= 0)
        {
            _wanderTime = 0.8f + Random.Shared.NextSingle();
            _wanderDir = Random.Shared.Next(4) switch
            {
                0 => Vector2.UnitX, 1 => -Vector2.UnitX, 2 => Vector2.UnitY, _ => -Vector2.UnitY,
            };
        }
        self.Vel = _wanderDir * self.Speed;
    }
}

/// <summary>Bounces around the room on a diagonal (used by the enraged boss).</summary>
public sealed class BounceMovement : IMovementStrategy
{
    private Vector2 _dir = Vector2.Normalize(new Vector2(1, 0.7f));
    private Vector2 _lastPos;

    public void Move(Enemy self, World world, float dt)
    {
        // If we did not move along an axis last frame we hit a wall: reflect.
        if (_lastPos != Vector2.Zero)
        {
            var moved = self.Pos - _lastPos;
            if (MathF.Abs(moved.X) < 0.01f) _dir.X = -_dir.X;
            if (MathF.Abs(moved.Y) < 0.01f) _dir.Y = -_dir.Y;
        }
        _lastPos = self.Pos;
        self.Vel = _dir * self.Speed * 1.6f;
    }
}

/// <summary>Shoots a single tear at the nearest player.</summary>
public sealed class AimedShotAttack(float cooldown, float tearSpeed = 300f) : IAttackStrategy
{
    private readonly float _cooldown = cooldown;
    private float _timer = cooldown * (0.5f + Random.Shared.NextSingle());

    public void Update(Enemy self, World world, float dt)
    {
        _timer -= dt;
        if (_timer > 0) return;
        var target = world.NearestPlayer(self.Pos);
        if (target == null) return;
        _timer = _cooldown;
        var dir = Vector2.Normalize(target.Pos - self.Pos + new Vector2(0.001f));
        world.SpawnTear(self.Pos, dir * tearSpeed, 1, 8f, 2.5f, fromEnemy: true);
    }
}

/// <summary>Shoots a ring of tears in all directions.</summary>
public sealed class RadialBurstAttack(int count, float cooldown, float tearSpeed = 240f) : IAttackStrategy
{
    private readonly float _cooldown = cooldown;
    private float _timer = cooldown;
    private float _rotation;

    public void Update(Enemy self, World world, float dt)
    {
        _timer -= dt;
        if (_timer > 0) return;
        _timer = _cooldown;
        _rotation += MathF.PI / count;
        for (int i = 0; i < count; i++)
        {
            float a = _rotation + i * MathF.Tau / count;
            var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
            world.SpawnTear(self.Pos + dir * self.Radius, dir * tearSpeed, 1, 9f, 3f, fromEnemy: true);
        }
    }
}

/// <summary>COMPOSITE of attack strategies, so an enemy can use several at once.</summary>
public sealed class CombinedAttack(params IAttackStrategy[] attacks) : IAttackStrategy
{
    public void Update(Enemy self, World world, float dt)
    {
        foreach (var attack in attacks) attack.Update(self, world, dt);
    }
}
