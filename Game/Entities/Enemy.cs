using System.Numerics;
using Isaac.Game.AI;

namespace Isaac.Game.Entities;

public enum EnemyKind : byte
{
    Fly,
    Gaper,
    Spitter,
    Charger,
    Spider,
    Monstro, // boss
}

/// <summary>
/// STRATEGY pattern, the Context: an enemy does not know how it moves or attacks.
/// It delegates to an <see cref="IMovementStrategy"/> and a list of <see cref="IAttackStrategy"/>,
/// which each subclass picks and which can be swapped at runtime with <see cref="SetBehaviour"/>.
/// </summary>
public abstract class Enemy
{
    public static readonly EnemyKind[] Regular = [EnemyKind.Fly, EnemyKind.Gaper, EnemyKind.Spitter, EnemyKind.Charger, EnemyKind.Spider];

    public EnemyKind Kind { get; }
    public Vector2 Pos;
    public Vector2 Vel;
    public float Radius;
    public float Hp;
    public float MaxHp;
    public float Speed;
    public bool Flying;       // ignores rocks
    public float HitFlash;
    public float Height;      // how far above the ground it is drawn, e.g. mid-jump
    public bool JustLanded;   // set for one frame when a jump ends

    private IMovementStrategy _movement;
    private IAttackStrategy[] _attacks;

    /// <summary>Health scales with the floor number.</summary>
    protected Enemy(EnemyKind kind, Vector2 pos, int floor, IMovementStrategy movement, params IAttackStrategy[] attacks)
    {
        Kind = kind;
        Pos = pos;
        (Radius, MaxHp, Speed, Flying) = kind switch
        {
            EnemyKind.Fly => (12f, 4f, 120f, true),
            EnemyKind.Gaper => (20f, 12f, 95f, false),
            EnemyKind.Spitter => (18f, 9f, 80f, false),
            EnemyKind.Charger => (18f, 10f, 90f, false),
            EnemyKind.Spider => (14f, 6f, 130f, false), // 195 px per 1.5 s move
            EnemyKind.Monstro => (48f, 160f, 70f, false),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        MaxHp *= 1f + (floor - 1) * 0.35f;
        Hp = MaxHp;
        _movement = movement;
        _attacks = attacks;
    }

    public bool IsBoss => Kind == EnemyKind.Monstro;
    public bool Dead => Hp <= 0;
    public IMovementStrategy Movement => _movement;
    public IReadOnlyList<IAttackStrategy> Attacks => _attacks;

    /// <summary>Replaces how this enemy moves and attacks.</summary>
    public void SetBehaviour(IMovementStrategy movement, params IAttackStrategy[] attacks)
    {
        _movement = movement;
        _attacks = attacks;
        // A jump cut short by the swap must not leave the enemy hanging in the air.
        Height = 0;
    }

    public void TakeDamage(float amount)
    {
        Hp -= amount;
        HitFlash = 0.08f;
        OnDamaged();
    }

    /// <summary>Called after every hit. Bosses use it to switch phases.</summary>
    protected virtual void OnDamaged()
    {
    }

    public void Update(World world, float dt)
    {
        HitFlash = MathF.Max(0, HitFlash - dt);
        JustLanded = false;
        var target = world.NearestPlayer(Pos);

        _movement.Move(this, target, world, dt);
        foreach (var attack in _attacks)
            attack.Attack(this, target, world, dt);

        Pos = world.MoveBody(Pos, Radius, Vel * dt, Flying);
    }
}

public class FlyEnemy(Vector2 pos, int floor)
    : Enemy(EnemyKind.Fly, pos, floor, new BuzzMovement());

public class GaperEnemy(Vector2 pos, int floor)
    : Enemy(EnemyKind.Gaper, pos, floor, new ChaseMovement());

public class SpitterEnemy(Vector2 pos, int floor)
    : Enemy(EnemyKind.Spitter, pos, floor, new TakeCoverMovement(220), new AimedShotAttack(1.8f, 300f));

public class ChargerEnemy(Vector2 pos, int floor)
    : Enemy(EnemyKind.Charger, pos, floor, new ChargeMovement());

public class SpiderEnemy(Vector2 pos, int floor)
    : Enemy(EnemyKind.Spider, pos, floor, new SkitterMovement(1.5f));

/// <summary>
/// Phase 1: bounces around the room and spits sprays of tears at the player.
/// Below half health it enrages: hops from tile to tile and fires a ring of tears on every landing instead.
/// </summary>
public class MonstroEnemy(Vector2 pos, int floor)
    : Enemy(
        EnemyKind.Monstro, pos, floor,
        new BounceMovement(1.6f),
        new SpitSprayAttack(count: 10, cooldown: 1.6f, spreadDegrees: 25f, minSpeed: 200f, maxSpeed: 360f),
        new AimedShotAttack(1.1f, 360f))
{
    private bool _enraged;

    protected override void OnDamaged()
    {
        if (_enraged || Hp >= MaxHp * 0.5f)
            return;
        _enraged = true;
        SetBehaviour(
            new JumpMovement(restTime: 1.0f, airTime: 0.7f, maxDistance: 260f, jumpHeight: 48f),
            new LandingRingAttack(10, 240f),
            new AimedShotAttack(0.8f, 380f));
    }
}

public sealed class BossRushFlyEnemy : FlyEnemy
{
    public BossRushFlyEnemy(Vector2 pos, int floor) : base(pos, floor)
    {
        MaxHp *= 1.5f;
        Hp = MaxHp;
        Speed *= 1.1f;
    }
}

public sealed class BossRushGaperEnemy : GaperEnemy
{
    public BossRushGaperEnemy(Vector2 pos, int floor) : base(pos, floor)
    {
        MaxHp *= 1.5f;
        Hp = MaxHp;
        Speed *= 1.1f;
    }
}

public sealed class BossRushSpitterEnemy : SpitterEnemy
{
    public BossRushSpitterEnemy(Vector2 pos, int floor) : base(pos, floor)
    {
        MaxHp *= 1.5f;
        Hp = MaxHp;
        Speed *= 1.1f;
    }
}

public sealed class BossRushSpiderEnemy : SpiderEnemy
{
    public BossRushSpiderEnemy(Vector2 pos, int floor) : base(pos, floor)
    {
        MaxHp *= 1.5f;
        Hp = MaxHp;
        Speed *= 1.1f;
    }
}

public sealed class BossRushMonstroEnemy : MonstroEnemy
{
    public BossRushMonstroEnemy(Vector2 pos, int floor) : base(pos, floor)
    {
        MaxHp *= 1.35f;
        Hp = MaxHp;
        Speed *= 1.15f;
    }
}
