using Unhallowed.PatternDemos.Demos;

namespace Unhallowed.PatternDemos;

/// <summary>Which of the three GoF families a pattern belongs to.</summary>
public enum PatternCategory
{
    Creational,
    Structural,
    Behavioral,
}

/// <summary>One row of the project's pattern plan.</summary>
/// <param name="Number">Catalogue number, matches docs/patterns/NN-*.md.</param>
/// <param name="Key">Command-line key.</param>
/// <param name="Title">Pattern name.</param>
/// <param name="Category">GoF family.</param>
/// <param name="GameUse">Where this pattern lands in Unhallowed.</param>
/// <param name="Owner">Team member reporting on it (requirement 11). Empty until claimed.</param>
/// <param name="Demo">The runnable demo, or null while the pattern is still unimplemented.</param>
public sealed record PatternEntry(
    int Number,
    string Key,
    string Title,
    PatternCategory Category,
    string GameUse,
    string Owner,
    IPatternDemo? Demo)
{
    public bool IsImplemented => Demo is not null;
}

/// <summary>
/// The full plan: all 23 GoF patterns mapped onto concrete Unhallowed features.
/// The course requires 22 - pick one to drop and mark it in docs/pattern-assignments.md.
/// </summary>
public static class PatternCatalog
{
    public static IReadOnlyList<PatternEntry> All { get; } =
    [
        // ---- Creational -------------------------------------------------------------------
        new(1, "singleton", "Singleton", PatternCategory.Creational,
            "ServiceRegistry: one service locator per process", "", new SingletonDemo()),
        new(2, "factory-method", "Factory Method", PatternCategory.Creational,
            "EnemyFactory.Create(archetype) replacing 'new Enemy' in RoomTypes.Populate", "", null),
        new(3, "abstract-factory", "Abstract Factory", PatternCategory.Creational,
            "IBiomeFactory: Basement/Caves produce matching enemies, tiles and props", "", null),
        new(4, "builder", "Builder", PatternCategory.Creational,
            "DungeonBuilder().WithFloor(2).WithRooms(12).WithBoss().Build()", "", null),
        new(5, "prototype", "Prototype", PatternCategory.Creational,
            "ItemPrototypeRegistry clones item definitions loaded from SQLite", "", null),

        // ---- Structural -------------------------------------------------------------------
        new(6, "adapter", "Adapter", PatternCategory.Structural,
            "GdiRenderSurface adapts System.Drawing.Graphics to IRenderSurface", "", new AdapterDemo()),
        new(7, "bridge", "Bridge", PatternCategory.Structural,
            "Weapon abstraction x IProjectileBehaviour (homing / piercing / bouncing)", "", null),
        new(8, "composite", "Composite", PatternCategory.Structural,
            "IHudElement tree: panels containing bars, labels and icons", "", null),
        new(9, "decorator", "Decorator", PatternCategory.Structural,
            "Item pickups wrap IPlayerStats: Damage Up, Speed Up, Rapid Fire", "", new DecoratorDemo()),
        new(10, "facade", "Facade", PatternCategory.Structural,
            "GameClient hides SignalR, JSON, reconnect and snapshot interpolation", "", null),
        new(11, "flyweight", "Flyweight", PatternCategory.Structural,
            "TileFlyweightFactory shares intrinsic tile data across thousands of tiles", "", null),
        new(12, "proxy", "Proxy", PatternCategory.Structural,
            "RemotePlayerProxy stands in for a player simulated on the server", "", null),

        // ---- Behavioral -------------------------------------------------------------------
        new(13, "chain-of-responsibility", "Chain of Responsibility", PatternCategory.Behavioral,
            "Damage pipeline: invulnerability -> shield -> armour -> health", "", null),
        new(14, "command", "Command", PatternCategory.Behavioral,
            "Player intents as objects, serialised to JSON and queued server-side", "", new CommandDemo()),
        new(15, "interpreter", "Interpreter", PatternCategory.Behavioral,
            "Item synergy mini-language: 'ON_HIT: burn(3) AND slow(0.5)'", "", null),
        new(16, "iterator", "Iterator", PatternCategory.Behavioral,
            "RoomIterator walks the dungeon graph breadth-first for minimap reveal", "", null),
        new(17, "mediator", "Mediator", PatternCategory.Behavioral,
            "CombatMediator coordinates player / enemy / pickup interactions", "", null),
        new(18, "memento", "Memento", PatternCategory.Behavioral,
            "WorldSnapshot drives both the network protocol and rollback", "", new MementoDemo()),
        new(19, "observer", "Observer", PatternCategory.Behavioral,
            "EventBus: HUD, audio, scoring and the SignalR relay all subscribe", "", new ObserverDemo()),
        new(20, "state", "State", PatternCategory.Behavioral,
            "Player states: Idle, Moving, Hurt (i-frames), Dead", "", new StateDemo()),
        new(21, "strategy", "Strategy", PatternCategory.Behavioral,
            "Enemy movement: Chaser, Wanderer, Charger, Stationary", "", new StrategyDemo()),
        new(22, "template-method", "Template Method", PatternCategory.Behavioral,
            "RoomBase.Enter fixes the lifecycle; room types fill in the steps", "", new TemplateMethodDemo()),
        new(23, "visitor", "Visitor", PatternCategory.Behavioral,
            "IEntityVisitor for collision resolution and end-of-run stat reports", "", null),
    ];

    public static PatternEntry? Find(string key)
        => All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
           ?? All.FirstOrDefault(p => p.Number.ToString() == key);

    public static IEnumerable<PatternEntry> Implemented => All.Where(p => p.IsImplemented);

    public static IEnumerable<PatternEntry> Pending => All.Where(p => !p.IsImplemented);
}
