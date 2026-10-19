using System.Numerics;

namespace Isaac.Game.Entities;

public enum EnemyRole
{
    Swarm,
    Chaser,
    Shooter,
    Rusher,
    Boss,
}

public readonly record struct EnemySpawn(EnemyRole Role, Vector2 Pos);

/// <summary>
/// ABSTRACT FACTORY pattern: creates a related family of enemies for a floor type.
/// Floor builders ask for roles, while the concrete factory decides which enemy
/// kind fills each role.
/// </summary>
public interface IEnemyFactory
{
    IReadOnlyList<EnemyRole> RegularRoles { get; }

    Enemy CreateSwarmEnemy(Vector2 pos, int floor);

    Enemy CreateChaserEnemy(Vector2 pos, int floor);

    Enemy CreateShooterEnemy(Vector2 pos, int floor);

    Enemy CreateRusherEnemy(Vector2 pos, int floor);

    Enemy CreateBoss(Vector2 pos, int floor);
}

public static class EnemyFactoryExtensions
{
    public static Enemy Create(this IEnemyFactory factory, EnemySpawn spawn, int floor) =>
        spawn.Role switch
        {
            EnemyRole.Swarm => factory.CreateSwarmEnemy(spawn.Pos, floor),
            EnemyRole.Chaser => factory.CreateChaserEnemy(spawn.Pos, floor),
            EnemyRole.Shooter => factory.CreateShooterEnemy(spawn.Pos, floor),
            EnemyRole.Rusher => factory.CreateRusherEnemy(spawn.Pos, floor),
            EnemyRole.Boss => factory.CreateBoss(spawn.Pos, floor),
            _ => throw new ArgumentOutOfRangeException(nameof(spawn)),
        };
}

/// <summary>Concrete factory for ordinary floors: mixed regular enemies and one Monstro boss.</summary>
public sealed class StandardEnemyFactory : IEnemyFactory
{
    private static readonly EnemyRole[] Roles =
    [
        EnemyRole.Swarm,
        EnemyRole.Chaser,
        EnemyRole.Shooter,
        EnemyRole.Rusher,
    ];

    public IReadOnlyList<EnemyRole> RegularRoles => Roles;

    public Enemy CreateSwarmEnemy(Vector2 pos, int floor) => new FlyEnemy(pos, floor);

    public Enemy CreateChaserEnemy(Vector2 pos, int floor) => new GaperEnemy(pos, floor);

    public Enemy CreateShooterEnemy(Vector2 pos, int floor) => new SpitterEnemy(pos, floor);

    public Enemy CreateRusherEnemy(Vector2 pos, int floor) => new ChargerEnemy(pos, floor);

    public Enemy CreateBoss(Vector2 pos, int floor) => new MonstroEnemy(pos, floor);
}

/// <summary>Concrete factory for boss rush floors: the enemy family is boss-focused.</summary>
public sealed class BossRushEnemyFactory : IEnemyFactory
{
    private static readonly EnemyRole[] Roles =
    [
        EnemyRole.Swarm,
        EnemyRole.Chaser,
        EnemyRole.Shooter,
        EnemyRole.Boss,
    ];

    public IReadOnlyList<EnemyRole> RegularRoles => Roles;

    public Enemy CreateSwarmEnemy(Vector2 pos, int floor) => new BossRushFlyEnemy(pos, floor);

    public Enemy CreateChaserEnemy(Vector2 pos, int floor) => new BossRushGaperEnemy(pos, floor);

    public Enemy CreateShooterEnemy(Vector2 pos, int floor) => new BossRushSpitterEnemy(pos, floor);

    public Enemy CreateRusherEnemy(Vector2 pos, int floor) => new BossRushGaperEnemy(pos, floor);

    public Enemy CreateBoss(Vector2 pos, int floor) => new BossRushMonstroEnemy(pos, floor);
}
