using System.Numerics;
using Isaac.Game.AI;

namespace Isaac.Game.Entities;

/// <summary>
/// FACTORY pattern: the only place that knows how each enemy kind is put
/// together (stats + which movement/attack strategies it uses).
/// Health scales with the floor number.
/// </summary>
public static class EnemyFactory
{
    public static readonly EnemyKind[] Regular = [EnemyKind.Fly, EnemyKind.Gaper, EnemyKind.Spitter, EnemyKind.Charger];

    public static Enemy Create(EnemyKind kind, Vector2 pos, int floor)
    {
        float scale = 1f + (floor - 1) * 0.35f;
        Enemy e = kind switch
        {
            EnemyKind.Fly => new Enemy
            {
                Kind = kind, Radius = 12, MaxHp = 4, Speed = 120, Flying = true,
                Movement = new BuzzMovement(),
            },
            EnemyKind.Gaper => new Enemy
            {
                Kind = kind, Radius = 20, MaxHp = 12, Speed = 95,
                Movement = new ChaseMovement(),
            },
            EnemyKind.Spitter => new Enemy
            {
                Kind = kind, Radius = 18, MaxHp = 9, Speed = 80,
                Movement = new KeepDistanceMovement(220),
                Attack = new AimedShotAttack(1.8f),
            },
            EnemyKind.Charger => new Enemy
            {
                Kind = kind, Radius = 18, MaxHp = 10, Speed = 90,
                Movement = new ChargeMovement(),
            },
            EnemyKind.Monstro => CreateBoss(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        e.MaxHp *= scale;
        e.Hp = e.MaxHp;
        e.Pos = pos;
        return e;
    }

    private static Enemy CreateBoss()
    {
        var boss = new Enemy
        {
            Kind = EnemyKind.Monstro, Radius = 48, MaxHp = 160, Speed = 70,
            Movement = new ChaseMovement(),
            Attack = new CombinedAttack(new RadialBurstAttack(10, 2.2f), new AimedShotAttack(1.1f, 360f)),
        };
        // Phase 2 below half health: swap strategies at runtime.
        boss.OnDamaged = self =>
        {
            if (self.Hp < self.MaxHp * 0.5f && self.Movement is ChaseMovement)
            {
                self.Movement = new BounceMovement();
                self.Attack = new CombinedAttack(new RadialBurstAttack(14, 1.4f, 280f), new AimedShotAttack(0.8f, 380f));
            }
        };
        return boss;
    }
}
