using System.Numerics;
using Isaac.Core;
using Isaac.Game.Entities;

namespace Isaac.Game.Dungeon;

/// <summary>
/// FACADE pattern, the Facade: one simple entry point to the dungeon subsystems. Its clients
/// (<see cref="World"/> and <see cref="LootDropper"/>) ask for "floor 3", "the enemies of this
/// room" or "the reward for clearing it", and never touch the subsystems behind it:
/// <see cref="FloorDirector"/>, <see cref="StandardFloorBuilder"/>, <see cref="BossRushFloorBuilder"/>,
/// the floor's <see cref="IEnemyFactory"/> and <see cref="PickupFactory"/>.
/// </summary>
public sealed class DungeonFacade
{
    private const float HeartChance = 0.4f;

    private readonly FloorDirector _director = new();
    private readonly IFloorBuilder _standardBuilder;
    private readonly IFloorBuilder _bossRushBuilder;
    private readonly Random _rng;

    /// <param name="rng">Seeds the floor layouts and the loot, so the same seed gives the same dungeon.</param>
    public DungeonFacade(Random? rng = null)
    {
        _rng = rng ?? Random.Shared;
        _standardBuilder = new StandardFloorBuilder(rng?.Next());
        _bossRushBuilder = new BossRushFloorBuilder(rng?.Next());
    }

    /// <summary>Builds floor <paramref name="number"/>; every <see cref="Config.BossRushEvery"/>th one is a boss rush.</summary>
    public Floor CreateFloor(int number)
    {
        var builder = number % Config.BossRushEvery == 0 ? _bossRushBuilder : _standardBuilder;
        return _director.Construct(builder, number);
    }

    /// <summary>The enemies waiting in <paramref name="room"/>, made by the floor's own enemy family. None once it is cleared.</summary>
    public IReadOnlyList<Enemy> SpawnEnemies(Floor floor, Room room) =>
        room.Cleared ? [] : room.Spawns.Select(spawn => floor.EnemyFactory.Create(spawn, floor.Number)).ToList();

    /// <summary>Marks <paramref name="room"/> as cleared and puts the reward for it into the room.</summary>
    public void DropClearReward(Floor floor, Room room)
    {
        room.Cleared = true;
        // The trapdoor only appears after the floor's last boss (boss rush floors have several).
        if (room.Kind == RoomKind.Boss && floor.BossRoomsLeft == 0)
        {
            room.Pickups.Add(PickupFactory.CreateTrapdoor(Room.Center));
            room.Pickups.Add(PickupFactory.CreateRandomItemPedestal(Room.Center + new Vector2(0, 110), _rng));
        }
        else if (room.Kind == RoomKind.Boss || _rng.NextSingle() < HeartChance)
        {
            // Earlier bosses in a boss rush always leave a heart for the next fight.
            room.Pickups.Add(PickupFactory.CreateHeart(Room.Center));
        }
    }
}
