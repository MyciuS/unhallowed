using Isaac.Game.Characters;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Game.Commands;

// COMMAND pattern, the Concrete Commands: the dev console's cheats. Each one keeps its receiver
// (the World, and the Player it acts on) in fields, calls it to do the work, and saves what it
// needs to undo the work just before.

/// <summary>Gives a player an item. Undo puts back the loadout and health the player had before.</summary>
public sealed class GiveItemCommand(World world, Player player, ItemKind item) : ICommand
{
    private readonly World _world = world;
    private readonly Player _player = player;
    private readonly ItemKind _item = item;
    private Character? _before;
    private int _healthBefore;

    public string Name => $"Give {ItemCatalog.Name(_item)} to P{_player.Id + 1}";

    public void Execute()
    {
        // A deep copy (PROTOTYPE pattern), so the item does not change the saved stats too.
        _before = _player.Character.DeepClone();
        _healthBefore = _player.Health;
        _world.GiveItem(_player, _item);
    }

    public void Undo()
    {
        _player.RestoreLoadout(_before!, _healthBefore);
    }
}

/// <summary>Refills a player's health, reviving them if they are down. Undo puts the old health back.</summary>
public sealed class HealCommand(World world, Player player) : ICommand
{
    private readonly World _world = world;
    private readonly Player _player = player;
    private int _healthBefore;

    public string Name => $"Heal P{_player.Id + 1}";

    public void Execute()
    {
        _healthBefore = _player.Health;
        _world.Heal(_player);
    }

    // If this brings every player back to 0 health, the World ends the game again on its next tick.
    public void Undo() => _player.Health = _healthBefore;
}

public enum Cheat
{
    GodMode,
    OneShotKills,
}

/// <summary>Switches a cheat on or off. Undo switches it back.</summary>
public sealed class ToggleCheatCommand(World world, Cheat cheat) : ICommand
{
    private readonly World _world = world;
    private readonly Cheat _cheat = cheat;

    public string Name => $"Toggle {(_cheat == Cheat.GodMode ? "god mode" : "one-shot kills")}";

    public void Execute() => Flip();

    public void Undo() => Flip();

    private void Flip()
    {
        if (_cheat == Cheat.GodMode)
            _world.GodMode = !_world.GodMode;
        else
            _world.OneShotKills = !_world.OneShotKills;
    }
}

/// <summary>Shows every room of the floor on the minimap. Undo hides the rooms that were unexplored again.</summary>
public sealed class RevealMapCommand(World world) : ICommand
{
    private readonly World _world = world;
    private List<Room> _hidden = [];

    public string Name => "Reveal map";

    public void Execute()
    {
        _hidden = _world.Floor.Rooms.Values.Where(r => !r.Visited).ToList();
        _world.RevealMap();
    }

    public void Undo()
    {
        foreach (var room in _hidden)
            room.Visited = false;
    }
}
