namespace Isaac.Demos;

/// <summary>Console demonstrations of the design patterns: <c>dotnet run -- --demo &lt;pattern&gt;</c>.</summary>
public static class PatternDemos
{
    private static readonly Dictionary<string, Action> All = new(StringComparer.OrdinalIgnoreCase)
    {
        ["prototype"] = PrototypeDemo.Run,
        ["bridge"] = BridgeDemo.Run,
    };

    public static void Run(string name)
    {
        if (All.TryGetValue(name, out var demo))
            demo();
        else
            Console.WriteLine($"Unknown demo '{name}'. Available: {string.Join(", ", All.Keys)}");
    }
}
