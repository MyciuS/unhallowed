using Isaac.Core;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Game;

// OBSERVER pattern, the Concrete Observers: independent systems that react to
// world events by implementing IEventListener<T> for each event they care about.
// None of them is referenced by the code that raises the events.

/// <summary>Keeps the team score.</summary>
public sealed class ScoreKeeper
    : IEventListener<EnemyKilled>,
        IEventListener<RoomCleared>,
        IEventListener<ItemPickedUp>,
        IEventListener<FloorStarted>
{
    public int Score { get; private set; }

    public ScoreKeeper(EventBus events)
    {
        events.Subscribe<EnemyKilled>(this);
        events.Subscribe<RoomCleared>(this);
        events.Subscribe<ItemPickedUp>(this);
        events.Subscribe<FloorStarted>(this);
    }

    public void Reset() => Score = 0;

    public void OnEvent(EnemyKilled e) => Score += e.Enemy.IsBoss ? 500 : 10 + (int)e.Enemy.MaxHp;

    public void OnEvent(RoomCleared e) => Score += 25;

    public void OnEvent(ItemPickedUp e) => Score += 50;

    public void OnEvent(FloorStarted e)
    {
        if (e.Floor.Number > 1)
            Score += 250;
    }
}

/// <summary>Shows on-screen messages for notable events.</summary>
public sealed class Announcer
    : IEventListener<ItemPickedUp>,
        IEventListener<PlayerDied>,
        IEventListener<FloorStarted>,
        IEventListener<RoomCleared>
{
    private readonly World _world;

    public Announcer(World world, EventBus events)
    {
        _world = world;
        events.Subscribe<ItemPickedUp>(this);
        events.Subscribe<PlayerDied>(this);
        events.Subscribe<FloorStarted>(this);
        events.Subscribe<RoomCleared>(this);
    }

    public void OnEvent(ItemPickedUp e) =>
        Say($"P{e.Player.Id + 1} picked up {ItemCatalog.Name(e.Item)}!");

    public void OnEvent(PlayerDied e) => Say($"P{e.Player.Id + 1} died!");

    public void OnEvent(FloorStarted e) => Say(e.Floor.Title);

    public void OnEvent(RoomCleared e)
    {
        if (e.Room.Kind != RoomKind.Boss)
            return;
        int left = e.Floor.BossRoomsLeft;
        Say(left == 0 ? "Boss defeated! Find the trapdoor." : $"Boss defeated! {left} to go.");
    }

    private void Say(string text) => _world.ShowMessage(text);
}

/// <summary>Drops rewards when rooms are cleared. A client of the <see cref="DungeonFacade"/>.</summary>
public sealed class LootDropper : IEventListener<RoomCleared>
{
    private readonly DungeonFacade _dungeon;

    public LootDropper(EventBus events, DungeonFacade dungeon)
    {
        _dungeon = dungeon;
        events.Subscribe(this);
    }

    public void OnEvent(RoomCleared e) => _dungeon.DropClearReward(e.Floor, e.Room);
}

/// <summary>Writes player actions and world events to the <see cref="GameLogger"/>.</summary>
public sealed class ActionLogger
    : IEventListener<FloorStarted>,
        IEventListener<EnemyKilled>,
        IEventListener<RoomCleared>,
        IEventListener<ItemPickedUp>,
        IEventListener<PlayerHurt>,
        IEventListener<PlayerDied>
{
    private readonly GameLogger _log = GameLogger.Instance;

    public ActionLogger(EventBus events)
    {
        events.Subscribe<FloorStarted>(this);
        events.Subscribe<EnemyKilled>(this);
        events.Subscribe<RoomCleared>(this);
        events.Subscribe<ItemPickedUp>(this);
        events.Subscribe<PlayerHurt>(this);
        events.Subscribe<PlayerDied>(this);
    }

    public void OnEvent(FloorStarted e) => _log.Log($"{e.Floor.Title} started");

    public void OnEvent(EnemyKilled e) => _log.Log($"{e.Enemy.Kind} killed");

    public void OnEvent(RoomCleared e) => _log.Log($"{e.Room.Kind} room cleared");

    public void OnEvent(ItemPickedUp e) =>
        _log.Log($"P{e.Player.Id + 1} picked up {ItemCatalog.Name(e.Item)}");

    public void OnEvent(PlayerHurt e) =>
        _log.Log($"P{e.Player.Id + 1} hurt, health {e.Player.Health}");

    public void OnEvent(PlayerDied e) => _log.Log($"P{e.Player.Id + 1} died");
}
