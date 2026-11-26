using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Game.Cheats;

/// <summary>
/// PROXY pattern, the Subject: all the cheats the dev console can use.
/// Both the real World and the CheatProxy have these methods.
/// </summary>
public interface ICheats
{
    bool GodMode { get; set; }
    bool OneShotKills { get; set; }

    void KillAllEnemies();
    void GiveItem(Player player, ItemKind item);
    void Heal(Player player);
    void SkipToNextFloor();
    bool EnterBossRoom();

    List<Room> RevealMap();
}
