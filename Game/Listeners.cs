using Isaac.Core;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Game;

// OBSERVER pattern: independent systems that react to world events.
// None of them is referenced by the code that raises the events.

/// <summary>Keeps the team score.</summary>
public sealed class ScoreKeeper
{
    public int Score { get; private set; }

    public ScoreKeeper(EventBus events)
    {
        events.Subscribe<EnemyKilled>(e => Score += e.Enemy.IsBoss ? 500 : 10 + (int)e.Enemy.MaxHp);
        events.Subscribe<RoomCleared>(_ => Score += 25);
        events.Subscribe<ItemPickedUp>(_ => Score += 50);
        events.Subscribe<FloorStarted>(e => { if (e.Floor.Number > 1) Score += 250; });
    }

    public void Reset() => Score = 0;
}

/// <summary>Shows on-screen messages for notable events.</summary>
public sealed class Announcer
{
    private readonly World _world;

    public Announcer(World world, EventBus events)
    {
        _world = world;
        events.Subscribe<ItemPickedUp>(e => Say($"P{e.Player.Id + 1} picked up {ItemCatalog.Name(e.Item)}!"));
        events.Subscribe<PlayerDied>(e => Say($"P{e.Player.Id + 1} died!"));
        events.Subscribe<FloorStarted>(e => Say(e.Floor.Title));
        events.Subscribe<RoomCleared>(e =>
        {
            if (e.Room.Kind != RoomKind.Boss) return;
            int left = e.Floor.BossRoomsLeft;
            Say(left == 0 ? "Boss defeated! Find the trapdoor." : $"Boss defeated! {left} to go.");
        });
    }

    private void Say(string text) => _world.ShowMessage(text);
}

/// <summary>Drops rewards when rooms are cleared.</summary>
public sealed class LootDropper
{
    public LootDropper(EventBus events)
    {
        events.Subscribe<RoomCleared>(e =>
        {
            var room = e.Room;
            // The trapdoor only appears after the floor's last boss (boss rush floors have several).
            if (room.Kind == RoomKind.Boss && e.Floor.BossRoomsLeft == 0)
            {
                room.Pickups.Add(PickupFactory.CreateTrapdoor(Room.Center));
                room.Pickups.Add(
                    PickupFactory.CreateRandomItemPedestal(
                        Room.Center + new System.Numerics.Vector2(0, 110),
                        Random.Shared));
            }
            else if (room.Kind == RoomKind.Boss || Random.Shared.NextSingle() < 0.4f)
            {
                // Earlier bosses in a boss rush always leave a heart for the next fight.
                room.Pickups.Add(PickupFactory.CreateHeart(Room.Center));
            }
        });
    }
}

/// <summary>Writes player actions and world events to the <see cref="GameLogger"/>.</summary>
public sealed class ActionLogger
{
    public ActionLogger(EventBus events)
    {
        var log = GameLogger.Instance;
        events.Subscribe<FloorStarted>(e => log.Log($"{e.Floor.Title} started"));
        events.Subscribe<EnemyKilled>(e => log.Log($"{e.Enemy.Kind} killed"));
        events.Subscribe<RoomCleared>(e => log.Log($"{e.Room.Kind} room cleared"));
        events.Subscribe<ItemPickedUp>(e => log.Log($"P{e.Player.Id + 1} picked up {ItemCatalog.Name(e.Item)}"));
        events.Subscribe<PlayerHurt>(e => log.Log($"P{e.Player.Id + 1} hurt, health {e.Player.Health}"));
        events.Subscribe<PlayerDied>(e => log.Log($"P{e.Player.Id + 1} died"));
    }
}
