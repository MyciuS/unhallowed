using System.Reflection;
using Isaac.Demos;

namespace Isaac.DevTools;

/// <summary>
/// The twelve patterns the course requires. Whether each one is in this build is found by
/// reflection, so the list stays right as team branches get merged.
/// </summary>
public static class PatternCatalog
{
    public static readonly IReadOnlyList<PatternInfo> All =
    [
        new("a", "Singleton", "Show that it is thread safe",
            "Core/GameLogger.cs", ["GameLogger"]),
        new("b", "Factory", "At least 3 classes in the product family",
            "Game/Entities/PickupFactory.cs, Pickup.cs", ["PickupFactory"]),
        new("c", "Abstract Factory", "At least 2 concrete factories, at least 3 classes in each family",
            "Game/Entities/EnemyFactory.cs, Enemy.cs", ["IEnemyFactory"]),
        new("d", "Strategy", "At least 4 strategy classes",
            "Game/AI/", ["IMovementStrategy"]),
        new("e", "Observer", "Show how it works with a sequence diagram",
            "Core/EventBus.cs, Game/Events.cs, Game/Listeners.cs", ["EventBus", "IEventListener"]),
        new("f", "Builder", "At least 2 concrete builders",
            "Game/Dungeon/FloorDirector.cs, *FloorBuilder.cs", ["FloorDirector", "IFloorBuilder"]),
        new("g", "Adapter", "Adapter and Adaptee have different numbers of methods",
            "Core/GameLoggerAdapter.cs", ["GameLoggerAdapter"]),
        new("h", "Prototype", "Compare deep and shallow copies; report the memory addresses",
            "Core/IPrototype.cs, Game/Characters/", ["IPrototype", "CharacterRegistry"]),
        new("i", "Decorator", "At least 3 levels of decoration",
            "Game/Items/ItemDecorators.cs", ["StatsDecorator"]),
        new("j", "Command", "Commands can be undone with Undo()",
            "-", ["Undo()"]),
        new("k", "Facade", "At least 2 client classes and at least 3 subsystem classes",
            "Game/Dungeon/DungeonFacade.cs", ["DungeonFacade"]),
        new("l", "Bridge", "At least 2 abstractions and 2 implementations; explain how it differs from Strategy and Adapter",
            "Rendering/HudWidget.cs, ICanvas.cs", ["HudWidget", "ICanvas"]),
    ];

    private static readonly HashSet<string> Names = CollectNames();

    public static bool IsInBuild(PatternInfo pattern) => pattern.Markers.All(Names.Contains);

    /// <summary>The <c>--demo</c> name of <paramref name="pattern"/>, or null if it has no demo yet.</summary>
    public static string? DemoFor(PatternInfo pattern) =>
        PatternDemos.Names.FirstOrDefault(name => Letters(name) == Letters(pattern.Name));

    private static string Letters(string s) => new(s.Where(char.IsLetter).Select(char.ToLowerInvariant).ToArray());

    private static HashSet<string> CollectNames()
    {
        const BindingFlags declared = BindingFlags.Public | BindingFlags.NonPublic |
                                      BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var names = new HashSet<string>();
        foreach (var type in typeof(PatternCatalog).Assembly.GetTypes())
        {
            names.Add(type.Name.Split('`')[0]);
            foreach (var method in type.GetMethods(declared))
                names.Add(method.Name + "()");
        }
        return names;
    }
}
