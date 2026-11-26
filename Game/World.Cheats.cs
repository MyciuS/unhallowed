using Isaac.Game.Cheats;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Game;

/// <summary>
/// Developer cheats for the F6 console. Only code running next to this World can call them, which
/// means only the host's own console: a remote client can send nothing but movement and shooting.
/// PROXY pattern, the Real Subject: the console reaches these through a <see cref="CheatProxy"/>.
/// </summary>
public sealed partial class World : ICheats
{
    public bool GodMode { get; set; }

    public bool OneShotKills { get; set; }

    public void KillAllEnemies()
    {
        var killed = Enemies.ToList();
        Enemies.Clear();
        foreach (var e in killed)
            Events.Publish(new EnemyKilled(e));
    }

    public void GiveItem(Player player, ItemKind item)
    {
        player.GiveItem(item);
        Events.Publish(new ItemPickedUp(player, item));
    }

    public void Heal(Player player)
    {
        player.Health = player.Stats.MaxHealth;
        GameOver = false;
    }

    public void SkipToNextFloor() => StartFloor(Floor.Number + 1);

    public bool EnterBossRoom()
    {
        var boss = Floor.Rooms.Values.FirstOrDefault(r =>
            r.Kind == RoomKind.Boss && !r.Cleared && r != Room
        );
        if (boss == null)
            return false;

        EnterRoom(boss, Direction.Up);
        return true;
    }

    public List<Room> RevealMap()
    {
        var hidden = Floor.Rooms.Values.Where(r => !r.Visited).ToList();
        foreach (var room in hidden)
            room.Visited = true;
        return hidden;
    }
}
