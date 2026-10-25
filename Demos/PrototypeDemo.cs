using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Isaac.Core;
using Isaac.Game.Characters;
using Isaac.Game.Items;

namespace Isaac.Demos;

/// <summary>
/// PROTOTYPE pattern demo. Clones a character both ways, prints the memory addresses of the
/// clone and of the objects it refers to, then shows what each copy does when the clone
/// picks up an item.
/// </summary>
public static class PrototypeDemo
{
    public static void Run()
    {
        Console.WriteLine("PROTOTYPE: characters are cloned from prototypes in a CharacterRegistry");
        ShowRegistry();
        Compare("Shallow copy (MemberwiseClone)", c => c.ShallowClone());
        Compare("Deep copy", c => c.DeepClone());
        CloneThroughInterface();
        CompareSpeed();
    }

    private static void CloneThroughInterface()
    {
        Console.WriteLine();
        Console.WriteLine("== Cloning through the Prototype interface ==");
        Console.WriteLine("  the caller only has IPrototype<object> references and never names a concrete class");
        IPrototype<object>[] prototypes = [new Character("Isaac", new Stats()), new Stats { Damage = 9f }];
        foreach (var prototype in prototypes)
        {
            object copy = prototype.DeepClone();
            Console.WriteLine($"  {prototype.GetType().Name,-10} -> the copy is a {copy.GetType().Name,-10} new object: {!ReferenceEquals(prototype, copy)}");
        }
    }

    private static void CompareSpeed()
    {
        const int count = 50_000;
        var prototype = new Character("Cain", new Stats { Speed = 300f, Damage = 3f });
        prototype.AddItem(ItemKind.RangeUp);

        Console.WriteLine();
        Console.WriteLine($"== Speed: making {count:N0} copies ==");
        Console.WriteLine($"  {"method",-38}{"best ms",10}{"ns / copy",12}{"bytes / copy",14}");
        Measure("ShallowClone()  (MemberwiseClone)", count, () => prototype.ShallowClone());
        Measure("DeepClone()     (prototype method)", count, () => prototype.DeepClone());
        Measure("new Character(original)  (copy ctor)", count, () => new Character(prototype));
        Measure("JSON serialize + deserialize", count, () => ThroughJson(prototype));
    }

    private static void Measure(string label, int count, Func<Character> copy)
    {
        // Repeat the run and keep the fastest, so JIT warm-up and the GC tuning itself to the
        // allocation rate don't decide the result.
        double bestMs = double.MaxValue;
        long bytes = 0;
        for (int round = 0; round < 5; round++)
        {
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
                copy();
            watch.Stop();
            bytes = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            bestMs = Math.Min(bestMs, watch.Elapsed.TotalMilliseconds);
        }
        Console.WriteLine($"  {label,-38}{bestMs,10:0.0}{bestMs * 1_000_000 / count,12:0}{bytes / count,14}");
    }

    private sealed record CharacterData(string Name, Stats Stats, List<ItemKind> Items);

    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true };

    // "Streaming": write the object out and read a brand-new one back.
    private static Character ThroughJson(Character c)
    {
        string json = JsonSerializer.Serialize(new CharacterData(c.Name, c.Stats, c.Items), Json);
        var data = JsonSerializer.Deserialize<CharacterData>(json, Json)!;
        return new Character(data.Name, data.Stats, data.Items);
    }

    private static void ShowRegistry()
    {
        var registry = CharacterRegistry.CreateDefault();
        Console.WriteLine();
        for (int id = 0; id < registry.Count; id++)
        {
            var c = registry.CreateForPlayer(id);
            Console.WriteLine(
                $"  P{id + 1} gets a clone of {c.Name,-10} damage {c.Stats.Damage,4:0.0}  speed {c.Stats.Speed,3:0}" +
                $"  max hp {c.Stats.MaxHealth,2}  items [{string.Join(", ", c.Items)}]");
        }
    }

    private static void Compare(string title, Func<Character, Character> copy)
    {
        var prototype = new Character("Cain", new Stats { Speed = 300f, Damage = 3f });
        prototype.AddItem(ItemKind.RangeUp);
        var clone = copy(prototype);

        Console.WriteLine();
        Console.WriteLine($"== {title} ==");
        Console.WriteLine($"  {"",-15}{"prototype",-21}{"clone",-21}same object?");
        var rows = ReadAddresses(
            ("Character", prototype, clone),
            ("Name (string)", prototype.Name, clone.Name),
            ("Stats", prototype.Stats, clone.Stats),
            ("Items (List)", prototype.Items, clone.Items));
        foreach (var (label, a, b, same) in rows)
            Console.WriteLine($"  {label,-15}0x{a:X16}   0x{b:X16}   {(same ? "YES (shared)" : "no")}");

        float damageBefore = prototype.Stats.Damage;
        string itemsBefore = string.Join(", ", prototype.Items);
        clone.AddItem(ItemKind.DamageUp);
        var next = copy(prototype);

        Console.WriteLine();
        Console.WriteLine("  the clone picks up Damage Up");
        Console.WriteLine($"  prototype damage {damageBefore:0.0} -> {prototype.Stats.Damage:0.0}, " +
                          $"items [{itemsBefore}] -> [{string.Join(", ", prototype.Items)}]");
        Console.WriteLine($"  the next clone starts with damage {next.Stats.Damage:0.0} and {next.Items.Count} item(s)");
        Console.WriteLine(ReferenceEquals(prototype.Stats, clone.Stats)
            ? "  => the pickup leaked into the prototype and into every later clone"
            : "  => the clone is independent: the prototype and later clones are unchanged");
    }

    private static List<(string Label, nint A, nint B, bool Same)> ReadAddresses(
        params (string Label, object A, object B)[] pairs)
    {
        var rows = new List<(string, nint, nint, bool)>(pairs.Length);
        // The GC may move objects at any moment, so the addresses are only comparable if they
        // are all read while it cannot run.
        bool paused = GC.TryStartNoGCRegion(1_000_000);
        try
        {
            foreach (var (label, a, b) in pairs)
                rows.Add((label, AddressOf(a), AddressOf(b), ReferenceEquals(a, b)));
        }
        finally
        {
            if (paused && GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
                GC.EndNoGCRegion();
        }
        return rows;
    }

    // A reference is the object's address; reinterpreting it as a number reveals that address.
    private static nint AddressOf(object o) => Unsafe.As<object, nint>(ref o);
}
