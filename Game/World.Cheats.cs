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
        // Removed right away: enemies only marked dead would still move, shoot and touch players
        // for one more tick before the sweep in UpdateTears.
        var killed = Enemies.ToList();
        Enemies.Clear();
        foreach (var e in killed)
            Events.Publish(new EnemyKilled(e));
    }

    /// <summary>Gives <paramref name="player"/> an item as if it was picked up.</summary>
    public void GiveItem(Player player, ItemKind item)
    {
        player.GiveItem(item);
        Events.Publish(new ItemPickedUp(player, item));
    }

    /// <summary>Refills <paramref name="player"/>'s health, reviving them if they are down, even after a game over.</summary>
    public void Heal(Player player)
    {
        player.Health = player.Stats.MaxHealth;
        GameOver = false;
    }

    public void SkipToNextFloor() => StartFloor(Floor.Number + 1);

    /// <returns>False when there is no other boss room left to fight on this floor.</returns>
    public bool EnterBossRoom()
    {
        // Skips the room the players are in: entering it again would respawn the boss at full health.
        var boss = Floor.Rooms.Values.FirstOrDefault(r => r.Kind == RoomKind.Boss && !r.Cleared && r != Room);
        if (boss == null) return false;

        // As if walking in through the bottom door, so players appear away from the boss.
        EnterRoom(boss, Direction.Up);
        return true;
    }

    public void RevealMap()
    {
        foreach (var room in Floor.Rooms.Values)
            room.Visited = true;
    }
}
