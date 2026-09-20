using Unhallowed.Core.AI;
using Unhallowed.Core.Commands;
using Unhallowed.Core.Common;
using Unhallowed.Core.Entities;
using Unhallowed.Core.Entities.Stats;
using Unhallowed.Core.Events;
using Unhallowed.Core.Rooms;
using Unhallowed.Core.Simulation;

namespace Unhallowed.PatternDemos.Demos;

/// <summary>01 - Singleton: one registry, thread-safe lazy creation, single access point.</summary>
public sealed class SingletonDemo : IPatternDemo
{
    public string Key => "singleton";

    public int Number => 1;

    public string Title => "Singleton";

    public string Summary => "ServiceRegistry hands the same instance to every caller.";

    public void Run()
    {
        var a = ServiceRegistry.Instance;
        var b = ServiceRegistry.Instance;

        DemoConsole.Step($"ServiceRegistry.Instance twice -> same object: {ReferenceEquals(a, b)}");

        a.Register(new EventBus());
        DemoConsole.Step("Registered an EventBus through reference 'a'");
        DemoConsole.Step($"Resolved through reference 'b': {b.TryResolve<EventBus>(out _)}");

        DemoConsole.Section("thread safety");
        var instances = new ServiceRegistry[32];
        Parallel.For(0, instances.Length, i => instances[i] = ServiceRegistry.Instance);
        DemoConsole.Step($"32 threads resolved {instances.Distinct().Count()} distinct instance(s)");

        a.Clear();
    }
}

/// <summary>19 - Observer: publishers never learn who is listening.</summary>
public sealed class ObserverDemo : IPatternDemo
{
    public string Key => "observer";

    public int Number => 19;

    public string Title => "Observer";

    public string Summary => "One EventBus, three unrelated observers, zero coupling.";

    private sealed class HudObserver : IGameEventObserver
    {
        public void OnNotify(GameEvent e) => DemoConsole.Note($"HUD      | {e.Message}");
    }

    private sealed class AudioObserver : IGameEventObserver
    {
        public void OnNotify(GameEvent e) => DemoConsole.Note($"AUDIO    | plays sfx for {e.Type}");
    }

    private sealed class ScoreObserver : IGameEventObserver
    {
        public int Kills { get; private set; }

        public void OnNotify(GameEvent e)
        {
            if (e.Type == GameEventType.EnemyDied)
            {
                Kills++;
            }
        }
    }

    public void Run()
    {
        var bus = new EventBus();
        var score = new ScoreObserver();

        bus.Attach(new HudObserver());
        bus.Attach(new AudioObserver());
        bus.Attach(score);

        DemoConsole.Step("Three observers attached. Publishing two events:");
        bus.Notify(new GameEvent(GameEventType.EnemyDied, 7, "gaper died"));
        bus.Notify(new GameEvent(GameEventType.ItemPickedUp, 1, "Ana picked up Damage Up"));

        DemoConsole.Section("detach");
        var hud = new HudObserver();
        bus.Attach(hud);
        bus.Detach(hud);
        bus.Notify(new GameEvent(GameEventType.EnemyDied, 9, "fly died"));

        DemoConsole.Step($"ScoreObserver counted {score.Kills} kills without the publisher knowing it exists");
    }
}

/// <summary>09 - Decorator: item effects stack by wrapping, not by editing Player.</summary>
public sealed class DecoratorDemo : IPatternDemo
{
    public string Key => "decorator";

    public int Number => 9;

    public string Title => "Decorator";

    public string Summary => "Picking up items builds a chain of stat wrappers at runtime.";

    public void Run()
    {
        IPlayerStats stats = new BaseStats();
        Print(stats);

        DemoConsole.Section("pick up Damage Up");
        stats = new DamageUp(stats);
        Print(stats);

        DemoConsole.Section("pick up Damage Up again (stacks)");
        stats = new DamageUp(stats);
        Print(stats);

        DemoConsole.Section("pick up Rapid Fire, then Heart Container");
        stats = new RapidFire(stats);
        stats = new HeartContainer(stats);
        Print(stats);

        DemoConsole.Section("the same thing through Player.PickUp");
        var player = new Player(1, "Ana", 0, Vec2.Zero);
        player.PickUp(inner => new SpeedUp(inner));
        player.PickUp(inner => new DamageUp(inner));
        DemoConsole.Step($"{player.DisplayName}: {player.Stats.Describe()}");
        DemoConsole.Note($"damage {player.Stats.Damage:0.0}, speed {player.Stats.MoveSpeed:0}");
    }

    private static void Print(IPlayerStats stats)
    {
        DemoConsole.Step(stats.Describe());
        DemoConsole.Note(
            $"damage {stats.Damage:0.0} | speed {stats.MoveSpeed:0} | " +
            $"fire rate {stats.FireRate:0.00}/s | max hp {stats.MaxHealth}");
    }
}

/// <summary>21 - Strategy: swap enemy movement without touching the Enemy class.</summary>
public sealed class StrategyDemo : IPatternDemo
{
    public string Key => "strategy";

    public int Number => 21;

    public string Title => "Strategy";

    public string Summary => "One Enemy class, four movement behaviours, chosen at runtime.";

    public void Run()
    {
        var bus = new EventBus();
        var world = new World(new NormalRoom("demo", new Vec2(400f, 300f), 1), bus);
        var player = world.AddPlayer("Ana", 0);
        player.Position = new Vec2(350f, 250f);

        IMovementStrategy[] strategies =
        [
            new ChaserStrategy(),
            new WandererStrategy(seed: 42),
            new ChargerStrategy(),
            new StationaryStrategy(),
        ];

        // 60 ticks is two seconds - long enough for the Charger to finish a wind-up and dash.
        const int totalTicks = 60;
        int[] sampleTicks = [0, 20, 40, 59];

        foreach (var strategy in strategies)
        {
            var enemy = new Enemy(world.NextEntityId(), "demo", new Vec2(50f, 50f), 10, 1, strategy);
            DemoConsole.Section(strategy.Name);

            for (var tick = 0; tick < totalTicks; tick++)
            {
                enemy.Update(1f / 30f, world);

                if (sampleTicks.Contains(tick))
                {
                    DemoConsole.Note(
                        $"tick {tick,2}: pos ({enemy.Position.X,6:0.0}, {enemy.Position.Y,6:0.0})  " +
                        $"speed {enemy.Velocity.Length,5:0.0}");
                }
            }
        }

        DemoConsole.Section("the point");
        DemoConsole.Step("Enemy.Update contains no if/switch on enemy type - it delegates one call.");
    }
}

/// <summary>20 - State: player behaviour changes by swapping the state object.</summary>
public sealed class StateDemo : IPatternDemo
{
    public string Key => "state";

    public int Number => 20;

    public string Title => "State";

    public string Summary => "Idle -> Moving -> Hurt -> Dead, each an object, no status enum.";

    public void Run()
    {
        var bus = new EventBus();
        var world = new World(new NormalRoom("demo", new Vec2(400f, 300f), 1), bus);
        var player = world.AddPlayer("Ana", 0);
        const float dt = 1f / 30f;

        DemoConsole.Step($"start: {player.StateName}");

        player.MoveIntent = new Vec2(1f, 0f);
        player.Update(dt, world);
        player.Update(dt, world);
        DemoConsole.Step($"after move input: {player.StateName}");

        player.TakeDamage(1);
        DemoConsole.Step($"after taking a hit: {player.StateName} (hp {player.Health}/{player.MaxHealth})");

        DemoConsole.Section("invulnerability window");
        player.TakeDamage(1);
        DemoConsole.Step($"immediate second hit ignored, hp still {player.Health}");

        for (var i = 0; i < 30; i++)
        {
            player.Update(dt, world);
        }

        DemoConsole.Step($"after i-frames expire: {player.StateName}");

        DemoConsole.Section("death");
        while (player.IsAlive)
        {
            player.TakeDamage(1);
            for (var i = 0; i < 30; i++)
            {
                player.Update(dt, world);
            }
        }

        player.Update(dt, world);
        DemoConsole.Step($"hp 0 -> {player.StateName}, accepts input: false");
    }
}

/// <summary>14 - Command: player intents are objects, so they queue, serialise and replay.</summary>
public sealed class CommandDemo : IPatternDemo
{
    public string Key => "command";

    public int Number => 14;

    public string Title => "Command";

    public string Summary => "Intents become objects: queued, deduplicated, replayed, sent as JSON.";

    public void Run()
    {
        var bus = new EventBus();
        var world = new World(new NormalRoom("demo", new Vec2(400f, 300f), 1), bus);
        var player = world.AddPlayer("Ana", 0);

        var queue = new CommandQueue();
        queue.Enqueue(new MoveCommand(1, new Vec2(1f, 0f)));
        queue.Enqueue(new ShootCommand(2, new Vec2(0f, -1f)));

        DemoConsole.Step("Enqueued Move(seq 1) and Shoot(seq 2)");

        DemoConsole.Section("duplicate and out-of-order packets");
        queue.Enqueue(new MoveCommand(1, new Vec2(-1f, 0f)));
        DemoConsole.Step("Re-sent seq 1 - dropped, because seq <= last accepted");

        var drained = queue.Drain();
        DemoConsole.Step($"Drained {drained.Count} command(s):");
        foreach (var command in drained)
        {
            DemoConsole.Note($"{command.Type} (seq {command.Sequence})");
            command.Execute(player, world);
        }

        DemoConsole.Note($"player MoveIntent is now ({player.MoveIntent.X}, {player.MoveIntent.Y})");

        DemoConsole.Section("the wire round-trip (requirement 6)");
        var dto = CommandTranslator.ToDto(new MoveCommand(3, new Vec2(0.7f, -0.7f)), 0.7f, -0.7f);
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        DemoConsole.Note($"serialised: {json}");

        var revived = CommandTranslator.FromDto(dto);
        DemoConsole.Note($"deserialised back into a {revived.GetType().Name}");

        DemoConsole.Section("unknown command types degrade safely");
        var unknown = CommandTranslator.FromDto(
            new Contracts.Messages.PlayerCommandDto(4, "Teleport", 0f, 0f));
        DemoConsole.Step($"'Teleport' from a rogue client -> {unknown.GetType().Name}, server unharmed");
    }
}

/// <summary>22 - Template Method: the room lifecycle is fixed, the steps vary.</summary>
public sealed class TemplateMethodDemo : IPatternDemo
{
    public string Key => "template-method";

    public int Number => 22;

    public string Title => "Template Method";

    public string Summary => "Every room type runs the same lifecycle, filling in different steps.";

    private sealed class LifecycleLogger : IGameEventObserver
    {
        public void OnNotify(GameEvent e) => DemoConsole.Note($"event: {e.Message}");
    }

    public void Run()
    {
        var size = new Vec2(960f, 540f);
        RoomBase[] rooms =
        [
            new NormalRoom("r1", size, seed: 7),
            new BossRoom("r2", size),
            new TreasureRoom("r3", size),
        ];

        foreach (var room in rooms)
        {
            var bus = new EventBus();
            bus.Attach(new LifecycleLogger());

            var world = new World(room, bus);
            world.AddPlayer("Ana", 0);

            DemoConsole.Section($"{room.RoomType} room");
            world.EnterRoom(room);

            var enemies = world.Entities.OfType<Enemy>().Count();
            DemoConsole.Step($"populated with {enemies} enemies, doors sealed: {room.DoorsSealed}");

            // Clear it out and let the back half of the template run.
            foreach (var enemy in world.Entities.OfType<Enemy>().ToArray())
            {
                enemy.TakeDamage(enemy.MaxHealth);
            }

            world.Step(1f / 30f);
            DemoConsole.Step($"after clearing -> cleared: {room.Cleared}, doors sealed: {room.DoorsSealed}");
        }

        DemoConsole.Section("the point");
        DemoConsole.Step("RoomBase.Enter is non-virtual: subclasses change the steps, never the order.");
    }
}

/// <summary>18 - Memento: the world captures and restores its own state.</summary>
public sealed class MementoDemo : IPatternDemo
{
    public string Key => "memento";

    public int Number => 18;

    public string Title => "Memento";

    public string Summary => "World snapshots drive both the network protocol and rollback.";

    public void Run()
    {
        var bus = new EventBus();
        var room = new NormalRoom("demo", new Vec2(960f, 540f), seed: 3);
        var world = new World(room, bus);
        world.EnterRoom(room);

        var player = world.AddPlayer("Ana", 0);
        var history = new SnapshotHistory();

        player.MoveIntent = new Vec2(1f, 0f);

        DemoConsole.Step("Simulating 30 ticks, capturing a memento each tick");
        for (var i = 0; i < 30; i++)
        {
            world.Step(1f / 30f);
            history.Push(world.Capture());
        }

        var atTick10 = history.FindByTick(10)!;
        var playerAt10 = atTick10.Entities.First(e => e.Id == player.Id);

        DemoConsole.Note($"history holds {history.Count} mementos");
        DemoConsole.Note($"tick 10 player x = {playerAt10.Position.X:0.0}");
        DemoConsole.Note($"tick 30 player x = {player.Position.X:0.0}");

        DemoConsole.Section("rollback (lag compensation)");
        world.Restore(atTick10);
        DemoConsole.Step($"after Restore(tick 10): world tick {world.Tick}, player x {player.Position.X:0.0}");

        DemoConsole.Section("the caretaker never looks inside");
        DemoConsole.Step("SnapshotHistory stores and returns WorldSnapshot objects and nothing else.");

        DemoConsole.Section("the same memento is the wire format (requirement 6)");
        var latest = history.Latest!;
        DemoConsole.Note($"tick {latest.Tick}, room {latest.RoomId}, {latest.Entities.Count} entities -> JSON");
    }
}
