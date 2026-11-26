using Isaac.Core;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Game.Cheats;

/// <summary>
/// PROXY pattern, the Proxy: has the same methods as the World, so the dev console can use it
/// instead of the World. Before passing a cheat on to the World it checks if cheats are locked,
/// and it writes every cheat to the log.
/// </summary>
public sealed class CheatProxy : ICheats
{
    private readonly ICheats _realCheats;

    public CheatProxy(ICheats realCheats)
    {
        _realCheats = realCheats;
    }

    /// <summary>When true, every cheat is blocked.</summary>
    public bool Locked { get; private set; }

    /// <summary>Locks the cheats. There is no unlock, so a locked game stays locked.</summary>
    public void Lock()
    {
        Locked = true;
        GameLogger.Instance.Log("Cheats locked");
    }

    private bool IsAllowed(string cheat)
    {
        if (Locked)
        {
            GameLogger.Instance.Log($"Cheat blocked: {cheat}");
            return false;
        }
        GameLogger.Instance.Log($"Cheat used: {cheat}");
        return true;
    }

    // Just reading if a cheat is on is fine, so only setting it is checked.
    public bool GodMode
    {
        get => _realCheats.GodMode;
        set
        {
            if (IsAllowed("god mode"))
                _realCheats.GodMode = value;
        }
    }

    public bool OneShotKills
    {
        get => _realCheats.OneShotKills;
        set
        {
            if (IsAllowed("one-shot kills"))
                _realCheats.OneShotKills = value;
        }
    }

    public void KillAllEnemies()
    {
        if (IsAllowed("kill all enemies"))
            _realCheats.KillAllEnemies();
    }

    public void GiveItem(Player player, ItemKind item)
    {
        if (IsAllowed($"give {ItemCatalog.Name(item)} to P{player.Id + 1}"))
            _realCheats.GiveItem(player, item);
    }

    public void Heal(Player player)
    {
        if (IsAllowed($"heal P{player.Id + 1}"))
            _realCheats.Heal(player);
    }

    public void SkipToNextFloor()
    {
        if (IsAllowed("next floor"))
            _realCheats.SkipToNextFloor();
    }

    public bool EnterBossRoom()
    {
        if (IsAllowed("go to boss room"))
            return _realCheats.EnterBossRoom();
        return false;
    }

    public List<Room> RevealMap()
    {
        if (IsAllowed("reveal map"))
            return _realCheats.RevealMap();
        return new List<Room>(); // nothing was revealed
    }
}
