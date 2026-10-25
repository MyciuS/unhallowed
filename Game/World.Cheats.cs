using System.Numerics;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Game;

/// <summary>
/// Developer cheats for the F6 console. Only code running next to this World can call them, which
/// means only the host's own console: a remote client can send nothing but movement and shooting.
/// </summary>
public sealed partial class World
{
    /// <summary>Players take no damage.</summary>
    public bool GodMode { get; set; }

    /// <summary>Any player tear kills the enemy it hits.</summary>
    public bool OneShotKills { get; set; }

    /// <summary>Kills every enemy in the room; they count as normal kills for score and loot.</summary>
    public void KillAllEnemies()
    {
        foreach (var e in Enemies)
            e.TakeDamage(e.Hp);
    }

    /// <summary>Gives <paramref name="player"/> an item as if it was picked up.</summary>
    public void GiveItem(Player player, ItemKind item)
    {
        player.GiveItem(item);
        Events.Publish(new ItemPickedUp(player, item));
    }

    /// <summary>Refills <paramref name="player"/>'s health, reviving them if they are down.</summary>
    public void Heal(Player player) => player.Health = player.Stats.MaxHealth;

    public void SkipToNextFloor() => StartFloor(Floor.Number + 1);

    /// <returns>False when every boss room on this floor is already cleared.</returns>
    public bool EnterBossRoom()
    {
        var boss = Floor.Rooms.Values.FirstOrDefault(r => r.Kind == RoomKind.Boss && !r.Cleared);
        if (boss == null) return false;

        EnterRoom(boss, null);
        var entry = Room.DoorEntry(Direction.Down);
        int i = 0;
        foreach (var p in Players.Values)
            p.Pos = entry + Vector2.UnitX * ((i++ - (Players.Count - 1) / 2f) * 48);
        return true;
    }

    public void RevealMap()
    {
        foreach (var room in Floor.Rooms.Values)
            room.Visited = true;
    }
}
