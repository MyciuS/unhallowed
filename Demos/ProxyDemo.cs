using Isaac.Game;
using Isaac.Game.Cheats;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Demos;

/// <summary>
/// PROXY pattern demo. Uses the cheats through a <see cref="CheatProxy"/>, locks it,
/// and shows that the same calls no longer reach the World.
/// </summary>
public static class ProxyDemo
{
    public static void Run()
    {
        Console.WriteLine("PROXY: the dev console uses the cheats through a CheatProxy, not the World");
        Console.WriteLine();
        Console.WriteLine("  Subject       ICheats");
        Console.WriteLine("  Real subject  World");
        Console.WriteLine("  Proxy         CheatProxy: checks the lock and logs, then calls the World");
        Console.WriteLine("  Client        DevConsole (F6 > Cheats), the cheat commands, and this demo");

        var world = new World();
        var player = world.AddPlayer(0);

        // The proxy has the same type as the World, so the code below works with either one.
        ICheats cheats = new CheatProxy(world);

        Console.WriteLine();
        Console.WriteLine("== Cheats unlocked ==");
        UseCheats(cheats, player);
        Show(world, player);

        Console.WriteLine();
        Console.WriteLine("== Cheats locked ==");
        ((CheatProxy)cheats).Lock();
        UseCheats(cheats, player);
        Show(world, player);
        Console.WriteLine("  Nothing changed: the proxy did not pass the calls on to the World.");
    }

    private static void UseCheats(ICheats cheats, Player player)
    {
        cheats.GiveItem(player, ItemKind.DamageUp);
        cheats.GodMode = !cheats.GodMode;
    }

    private static void Show(World world, Player player)
    {
        Console.WriteLine($"  World now: DMG {player.Stats.Damage:0.0}  ITEMS {player.Items.Count}  GOD {(world.GodMode ? "on" : "off")}");
    }
}
