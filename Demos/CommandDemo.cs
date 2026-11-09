using Isaac.Game;
using Isaac.Game.Commands;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Demos;

/// <summary>
/// COMMAND pattern demo. Runs the dev console's cheats as commands through a
/// <see cref="CommandHistory"/>, then undoes and redoes them, printing the player and world after each step.
/// </summary>
public static class CommandDemo
{
    public static void Run()
    {
        Console.WriteLine("COMMAND: each cheat is an object with Execute() and Undo()");
        Console.WriteLine();
        Console.WriteLine("  Command           ICommand: Name, Execute(), Undo()");
        Console.WriteLine("  Concrete commands GiveItemCommand, HealCommand, ToggleCheatCommand, RevealMapCommand");
        Console.WriteLine("  Receivers         World, Player");
        Console.WriteLine("  Invoker           CommandHistory: Execute, Undo, Redo (two stacks)");
        Console.WriteLine("  Client            DevConsole (F6 > Cheats), and this demo");

        var world = new World();
        var player = world.AddPlayer(0);
        var history = new CommandHistory();
        player.Health = 1; // hurt, so healing shows up

        Console.WriteLine();
        Console.WriteLine("== Execute ==");
        Show("start", world, player);
        Run(history, new GiveItemCommand(world, player, ItemKind.DamageUp), world, player);
        Run(history, new GiveItemCommand(world, player, ItemKind.TearsUp), world, player);
        Run(history, new GiveItemCommand(world, player, ItemKind.HeartContainer), world, player);
        Run(history, new HealCommand(world, player), world, player);
        Run(history, new ToggleCheatCommand(world, Cheat.GodMode), world, player);
        Run(history, new RevealMapCommand(world), world, player);

        Console.WriteLine();
        Console.WriteLine("== Undo, newest first ==");
        for (int i = 0; i < 4; i++)
            Show($"Undo: {history.Undo()!.Name}", world, player);

        Console.WriteLine();
        Console.WriteLine("== Redo ==");
        Show($"Redo: {history.Redo()!.Name}", world, player);

        Console.WriteLine();
        Console.WriteLine("== A new command clears what could still be redone ==");
        Console.WriteLine($"  before: can redo = {history.CanRedo}");
        Run(history, new GiveItemCommand(world, player, ItemKind.SpeedUp), world, player);
        Console.WriteLine($"  after:  can redo = {history.CanRedo}");
        Console.WriteLine();
        Console.WriteLine($"  History, newest first: {string.Join(" | ", history.Done.Select(c => c.Name))}");
    }

    private static void Run(CommandHistory history, ICommand command, World world, Player player)
    {
        history.Execute(command);
        Show(command.Name, world, player);
    }

    private static void Show(string step, World world, Player player)
    {
        int seen = world.Floor.Rooms.Values.Count(r => r.Visited);
        Console.WriteLine(
            $"  {step,-34} HP {player.Health,2}/{player.Stats.MaxHealth,-2}  DMG {player.Stats.Damage,4:0.0}" +
            $"  TEARS/S {1 / player.Stats.FireDelay,4:0.0}  ITEMS {player.Items.Count}" +
            $"  GOD {(world.GodMode ? "on " : "off")}  MAP {seen}/{world.Floor.Rooms.Count}");
    }
}
