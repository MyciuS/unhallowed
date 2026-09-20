using Unhallowed.Core.AI;
using Unhallowed.Core.Commands;
using Unhallowed.Core.Common;
using Unhallowed.Core.Entities;
using Unhallowed.Core.Entities.Stats;
using Unhallowed.Core.Events;
using Unhallowed.Core.Rooms;
using Unhallowed.Core.Simulation;
using Xunit;

namespace Unhallowed.Tests;

public sealed class SingletonTests
{
    [Fact]
    public void Instance_IsAlwaysTheSameObject()
    {
        Assert.Same(ServiceRegistry.Instance, ServiceRegistry.Instance);
    }

    [Fact]
    public void Instance_IsStableUnderConcurrentAccess()
    {
        var instances = new ServiceRegistry[64];
        Parallel.For(0, instances.Length, i => instances[i] = ServiceRegistry.Instance);

        Assert.Single(instances.Distinct());
    }

    [Fact]
    public void Resolve_ThrowsForUnregisteredService()
    {
        ServiceRegistry.Instance.Clear();
        Assert.Throws<InvalidOperationException>(() => ServiceRegistry.Instance.Resolve<EventBus>());
    }
}

public sealed class ObserverTests
{
    private sealed class CountingObserver : IGameEventObserver
    {
        public int Count { get; private set; }

        public void OnNotify(GameEvent gameEvent) => Count++;
    }

    [Fact]
    public void Notify_ReachesEveryAttachedObserver()
    {
        var bus = new EventBus();
        var a = new CountingObserver();
        var b = new CountingObserver();
        bus.Attach(a);
        bus.Attach(b);

        bus.Notify(new GameEvent(GameEventType.EnemyDied, 1, "x"));

        Assert.Equal(1, a.Count);
        Assert.Equal(1, b.Count);
    }

    [Fact]
    public void Detach_StopsDelivery()
    {
        var bus = new EventBus();
        var observer = new CountingObserver();
        bus.Attach(observer);
        bus.Detach(observer);

        bus.Notify(new GameEvent(GameEventType.EnemyDied, 1, "x"));

        Assert.Equal(0, observer.Count);
    }

    [Fact]
    public void Attach_IsIdempotent()
    {
        var bus = new EventBus();
        var observer = new CountingObserver();
        bus.Attach(observer);
        bus.Attach(observer);

        bus.Notify(new GameEvent(GameEventType.EnemyDied, 1, "x"));

        Assert.Equal(1, observer.Count);
    }
}

public sealed class DecoratorTests
{
    [Fact]
    public void BaseStats_AreUnmodified()
    {
        var stats = new BaseStats();
        Assert.Equal("Base", stats.Describe());
    }

    [Fact]
    public void Decorators_Stack()
    {
        IPlayerStats stats = new BaseStats();
        var baseDamage = stats.Damage;

        stats = new DamageUp(stats);
        stats = new DamageUp(stats);

        Assert.Equal(baseDamage + 3.0f, stats.Damage, 3);
        Assert.Equal("Base + Damage Up + Damage Up", stats.Describe());
    }

    [Fact]
    public void Decorator_ForwardsUntouchedStats()
    {
        IPlayerStats bare = new BaseStats();
        IPlayerStats decorated = new DamageUp(bare);

        Assert.Equal(bare.MoveSpeed, decorated.MoveSpeed);
        Assert.Equal(bare.MaxHealth, decorated.MaxHealth);
    }

    [Fact]
    public void PickUp_GrantsHeartContainersImmediately()
    {
        var player = new Player(1, "Ana", 0, Vec2.Zero);
        var startingMax = player.MaxHealth;

        player.PickUp(inner => new HeartContainer(inner));

        Assert.Equal(startingMax + 2, player.MaxHealth);
        Assert.Equal(player.MaxHealth, player.Health);
    }
}

public sealed class CommandTests
{
    [Fact]
    public void Queue_DropsDuplicateAndStaleSequences()
    {
        var queue = new CommandQueue();
        queue.Enqueue(new MoveCommand(1, new Vec2(1f, 0f)));
        queue.Enqueue(new MoveCommand(1, new Vec2(-1f, 0f)));
        queue.Enqueue(new MoveCommand(0, new Vec2(0f, 1f)));

        Assert.Single(queue.Drain());
    }

    [Fact]
    public void Drain_EmptiesTheQueue()
    {
        var queue = new CommandQueue();
        queue.Enqueue(new MoveCommand(1, Vec2.Zero));

        Assert.Single(queue.Drain());
        Assert.Empty(queue.Drain());
    }

    [Fact]
    public void MoveCommand_ClampsOverlongVectors()
    {
        var (world, player) = TestWorld.Create();

        new MoveCommand(1, new Vec2(100f, 0f)).Execute(player, world);

        Assert.Equal(1f, player.MoveIntent.Length, 3);
    }

    [Fact]
    public void Translator_MapsUnknownTypesToNoOp()
    {
        var command = CommandTranslator.FromDto(
            new Contracts.Messages.PlayerCommandDto(1, "Teleport", 0f, 0f));

        Assert.IsType<NoOpCommand>(command);
    }
}

public sealed class StateTests
{
    [Fact]
    public void Player_StartsIdle()
    {
        var (_, player) = TestWorld.Create();
        Assert.Equal("Idle", player.StateName);
    }

    [Fact]
    public void Player_EntersMovingOnInput()
    {
        var (world, player) = TestWorld.Create();
        player.MoveIntent = new Vec2(1f, 0f);

        player.Update(1f / 30f, world);
        player.Update(1f / 30f, world);

        Assert.Equal("Moving", player.StateName);
    }

    [Fact]
    public void HurtState_IgnoresFurtherDamage()
    {
        var (_, player) = TestWorld.Create();

        player.TakeDamage(1);
        var afterFirstHit = player.Health;
        player.TakeDamage(1);

        Assert.Equal("Hurt", player.StateName);
        Assert.Equal(afterFirstHit, player.Health);
    }

    [Fact]
    public void Player_ReachesDeadStateAtZeroHealth()
    {
        var (world, player) = TestWorld.Create();

        while (player.IsAlive)
        {
            player.TakeDamage(1);
            for (var i = 0; i < 30; i++)
            {
                player.Update(1f / 30f, world);
            }
        }

        player.Update(1f / 30f, world);

        Assert.Equal("Dead", player.StateName);
    }
}

public sealed class StrategyTests
{
    [Fact]
    public void Chaser_MovesTowardTheNearestPlayer()
    {
        var (world, player) = TestWorld.Create();
        player.Position = new Vec2(300f, 300f);

        var enemy = new Enemy(99, "gaper", new Vec2(50f, 50f), 10, 1, new ChaserStrategy());
        var before = Vec2.Distance(enemy.Position, player.Position);

        for (var i = 0; i < 10; i++)
        {
            enemy.Update(1f / 30f, world);
        }

        Assert.True(Vec2.Distance(enemy.Position, player.Position) < before);
    }

    [Fact]
    public void Stationary_NeverMoves()
    {
        var (world, _) = TestWorld.Create();
        var enemy = new Enemy(99, "turret", new Vec2(50f, 50f), 10, 1, new StationaryStrategy());

        for (var i = 0; i < 10; i++)
        {
            enemy.Update(1f / 30f, world);
        }

        Assert.Equal(new Vec2(50f, 50f), enemy.Position);
    }
}

public sealed class TemplateMethodTests
{
    [Fact]
    public void NormalRoom_SealsDoorsAndPopulates()
    {
        var room = new NormalRoom("r1", new Vec2(960f, 540f), seed: 7);
        var world = new World(room, new EventBus());
        world.AddPlayer("Ana", 0);
        world.EnterRoom(room);

        Assert.True(room.DoorsSealed);
        Assert.False(room.Cleared);
        Assert.NotEmpty(world.Entities.OfType<Enemy>());
    }

    [Fact]
    public void TreasureRoom_NeverSealsDoors()
    {
        var room = new TreasureRoom("r3", new Vec2(960f, 540f));
        var world = new World(room, new EventBus());
        world.EnterRoom(room);

        Assert.False(room.DoorsSealed);
        Assert.True(room.Cleared);
    }

    [Fact]
    public void Room_UnsealsOnceEveryEnemyIsDead()
    {
        var room = new NormalRoom("r1", new Vec2(960f, 540f), seed: 7);
        var world = new World(room, new EventBus());
        world.AddPlayer("Ana", 0);
        world.EnterRoom(room);

        foreach (var enemy in world.Entities.OfType<Enemy>().ToArray())
        {
            enemy.TakeDamage(enemy.MaxHealth);
        }

        world.Step(1f / 30f);

        Assert.True(room.Cleared);
        Assert.False(room.DoorsSealed);
    }
}

public sealed class MementoTests
{
    [Fact]
    public void Capture_RecordsEveryEntity()
    {
        var (world, _) = TestWorld.Create();
        var snapshot = world.Capture();

        Assert.Equal(world.Entities.Count, snapshot.Entities.Count);
    }

    [Fact]
    public void Restore_RewindsPositions()
    {
        var (world, player) = TestWorld.Create();
        player.MoveIntent = new Vec2(1f, 0f);

        var early = world.Capture();
        var startX = player.Position.X;

        for (var i = 0; i < 20; i++)
        {
            world.Step(1f / 30f);
        }

        Assert.True(player.Position.X > startX);

        world.Restore(early);

        Assert.Equal(startX, player.Position.X, 3);
    }

    [Fact]
    public void History_IsBounded()
    {
        var history = new SnapshotHistory(capacity: 4);
        for (var tick = 0; tick < 10; tick++)
        {
            history.Push(new WorldSnapshot(tick, "r1", []));
        }

        Assert.Equal(4, history.Count);
        Assert.Equal(9, history.Latest!.Tick);
        Assert.Null(history.FindByTick(0));
    }
}

/// <summary>Shared setup so each test does not rebuild the same three objects.</summary>
internal static class TestWorld
{
    public static (World World, Player Player) Create()
    {
        var room = new TreasureRoom("test", new Vec2(960f, 540f));
        var world = new World(room, new EventBus());
        var player = world.AddPlayer("Ana", 0);
        return (world, player);
    }
}
