using System.Numerics;

namespace Isaac.Game.Entities;

public enum EnemyKind : byte
{
    Fly,
    Gaper,
    Spitter,
    Charger,
    Monstro, // boss
}

public abstract class Enemy
{
    public static readonly EnemyKind[] Regular =
    [
        EnemyKind.Fly,
        EnemyKind.Gaper,
        EnemyKind.Spitter,
        EnemyKind.Charger,
    ];

    public EnemyKind Kind { get; }
    public Vector2 Pos;
    public Vector2 Vel;
    public float Radius;
    public float Hp;
    public float MaxHp;
    public float Speed;
    public bool Flying; // ignores rocks
    public float HitFlash;

    // Behaviour state. Each kind only uses some of these.
    private float _phase = Random.Shared.NextSingle() * MathF.Tau; // Fly wobble
    private readonly float _strafeSign = Random.Shared.Next(2) == 0 ? -1 : 1; // Spitter
    private Vector2 _dashDir; // Charger
    private float _dashTime;
    private Vector2 _wanderDir;
    private float _wanderTime;
    private Vector2 _bounceDir = Vector2.Normalize(new Vector2(1, 0.7f)); // enraged boss
    private Vector2 _lastPos;
    private float _shotTimer;
    private float _burstTimer;
    private float _burstRotation;
    private bool _enraged; // boss phase 2

    /// <summary>Health scales with the floor number.</summary>
    protected Enemy(EnemyKind kind, Vector2 pos, int floor)
    {
        Kind = kind;
        Pos = pos;
        (Radius, MaxHp, Speed, Flying) = kind switch
        {
            EnemyKind.Fly => (12f, 4f, 120f, true),
            EnemyKind.Gaper => (20f, 12f, 95f, false),
            EnemyKind.Spitter => (18f, 9f, 80f, false),
            EnemyKind.Charger => (18f, 10f, 90f, false),
            EnemyKind.Monstro => (48f, 160f, 70f, false),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        MaxHp *= 1f + (floor - 1) * 0.35f;
        Hp = MaxHp;
        _shotTimer =
            (kind == EnemyKind.Spitter ? 1.8f : 1.1f) * (0.5f + Random.Shared.NextSingle());
        _burstTimer = 2.2f;
    }

    public bool IsBoss => Kind == EnemyKind.Monstro;
    public bool Dead => Hp <= 0;

    public void TakeDamage(float amount)
    {
        Hp -= amount;
        HitFlash = 0.08f;
        // Monstro gets faster and angrier below half health.
        if (IsBoss && !_enraged && Hp < MaxHp * 0.5f)
        {
            _enraged = true;
            _shotTimer = 0.8f * (0.5f + Random.Shared.NextSingle());
            _burstTimer = 1.4f;
        }
    }

    public void Update(World world, float dt)
    {
        HitFlash = MathF.Max(0, HitFlash - dt);
        var target = world.NearestPlayer(Pos);

        switch (Kind)
        {
            case EnemyKind.Fly:
                Buzz(target, dt);
                break;
            case EnemyKind.Gaper:
                Chase(target);
                break;
            case EnemyKind.Spitter:
                KeepDistance(target, 220);
                AimedShot(world, target, dt, 1.8f, 300f);
                break;
            case EnemyKind.Charger:
                Charge(target, dt);
                break;
            case EnemyKind.Monstro when !_enraged:
                Chase(target);
                RadialBurst(world, dt, 10, 2.2f, 240f);
                AimedShot(world, target, dt, 1.1f, 360f);
                break;
            case EnemyKind.Monstro:
                Bounce();
                RadialBurst(world, dt, 14, 1.4f, 280f);
                AimedShot(world, target, dt, 0.8f, 380f);
                break;
        }

        Pos = world.MoveBody(Pos, Radius, Vel * dt, Flying);
    }

    // ------------------------------------------------------------- movement

    private static Vector2 Toward(Vector2 from, Vector2 to) =>
        Vector2.Normalize(to - from + new Vector2(0.001f));

    /// <summary>Walk straight at the nearest living player.</summary>
    private void Chase(Player? target)
    {
        Vel = target == null ? Vector2.Zero : Toward(Pos, target.Pos) * Speed;
    }

    /// <summary>Erratic buzzing towards the player, like Isaac's flies.</summary>
    private void Buzz(Player? target, float dt)
    {
        _phase += dt * 6f;
        var toward = target == null ? Vector2.Zero : Toward(Pos, target.Pos);
        var wobble = new Vector2(MathF.Cos(_phase), MathF.Sin(_phase * 1.3f));
        Vel = Vector2.Normalize(toward + wobble * 0.9f + new Vector2(0.001f)) * Speed;
    }

    /// <summary>Keeps a preferred distance from the player, strafing around them.</summary>
    private void KeepDistance(Player? target, float preferred)
    {
        if (target == null)
        {
            Vel = Vector2.Zero;
            return;
        }
        var delta = target.Pos - Pos;
        float dist = delta.Length();
        var dir = dist > 0.01f ? delta / dist : Vector2.UnitX;
        var strafe = new Vector2(-dir.Y, dir.X) * _strafeSign;
        float radial =
            dist > preferred + 40 ? 1
            : dist < preferred - 40 ? -1
            : 0;
        Vel = Vector2.Normalize(dir * radial + strafe * 0.6f) * Speed;
    }

    /// <summary>Wanders slowly; when lined up with a player, dashes at them.</summary>
    private void Charge(Player? target, float dt)
    {
        if (_dashTime > 0)
        {
            _dashTime -= dt;
            Vel = _dashDir * Speed * 3.2f;
            return;
        }

        if (target != null)
        {
            var d = target.Pos - Pos;
            if (MathF.Abs(d.X) < 24 || MathF.Abs(d.Y) < 24)
            {
                _dashDir =
                    MathF.Abs(d.X) < 24
                        ? new Vector2(0, MathF.Sign(d.Y))
                        : new Vector2(MathF.Sign(d.X), 0);
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
                0 => Vector2.UnitX,
                1 => -Vector2.UnitX,
                2 => Vector2.UnitY,
                _ => -Vector2.UnitY,
            };
        }
        Vel = _wanderDir * Speed;
    }

    /// <summary>Bounces around the room on a diagonal.</summary>
    private void Bounce()
    {
        // If we did not move along an axis last frame we hit a wall: reflect.
        if (_lastPos != Vector2.Zero)
        {
            var moved = Pos - _lastPos;
            if (MathF.Abs(moved.X) < 0.01f)
                _bounceDir.X = -_bounceDir.X;
            if (MathF.Abs(moved.Y) < 0.01f)
                _bounceDir.Y = -_bounceDir.Y;
        }
        _lastPos = Pos;
        Vel = _bounceDir * Speed * 1.6f;
    }

    // -------------------------------------------------------------- attacks

    /// <summary>Shoots a single tear at the nearest player.</summary>
    private void AimedShot(World world, Player? target, float dt, float cooldown, float tearSpeed)
    {
        _shotTimer -= dt;
        if (_shotTimer > 0 || target == null)
            return;
        _shotTimer = cooldown;
        world.SpawnTear(Pos, Toward(Pos, target.Pos) * tearSpeed, 1, 8f, 2.5f, fromEnemy: true);
    }

    /// <summary>Shoots a ring of tears in all directions.</summary>
    private void RadialBurst(World world, float dt, int count, float cooldown, float tearSpeed)
    {
        _burstTimer -= dt;
        if (_burstTimer > 0)
            return;
        _burstTimer = cooldown;
        _burstRotation += MathF.PI / count;
        for (int i = 0; i < count; i++)
        {
            float a = _burstRotation + i * MathF.Tau / count;
            var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
            world.SpawnTear(Pos + dir * Radius, dir * tearSpeed, 1, 9f, 3f, fromEnemy: true);
        }
    }
}

public class FlyEnemy(Vector2 pos, int floor) : Enemy(EnemyKind.Fly, pos, floor);

public class GaperEnemy(Vector2 pos, int floor) : Enemy(EnemyKind.Gaper, pos, floor);

public class SpitterEnemy(Vector2 pos, int floor) : Enemy(EnemyKind.Spitter, pos, floor);

public class ChargerEnemy(Vector2 pos, int floor) : Enemy(EnemyKind.Charger, pos, floor);

public class MonstroEnemy(Vector2 pos, int floor) : Enemy(EnemyKind.Monstro, pos, floor);

public sealed class BossRushFlyEnemy : FlyEnemy
{
    public BossRushFlyEnemy(Vector2 pos, int floor)
        : base(pos, floor)
    {
        MaxHp *= 1.5f;
        Hp = MaxHp;
        Speed *= 1.1f;
    }
}

public sealed class BossRushGaperEnemy : GaperEnemy
{
    public BossRushGaperEnemy(Vector2 pos, int floor)
        : base(pos, floor)
    {
        MaxHp *= 1.5f;
        Hp = MaxHp;
        Speed *= 1.1f;
    }
}

public sealed class BossRushSpitterEnemy : SpitterEnemy
{
    public BossRushSpitterEnemy(Vector2 pos, int floor)
        : base(pos, floor)
    {
        MaxHp *= 1.5f;
        Hp = MaxHp;
        Speed *= 1.1f;
    }
}

public sealed class BossRushMonstroEnemy : MonstroEnemy
{
    public BossRushMonstroEnemy(Vector2 pos, int floor)
        : base(pos, floor)
    {
        MaxHp *= 1.35f;
        Hp = MaxHp;
        Speed *= 1.15f;
    }
}
