using Isaac.Core;
using Isaac.Game;
using Isaac.Game.Dungeon;

namespace Isaac.Demos;

/// <summary>
/// FACADE pattern demo. Calls the three methods of <see cref="DungeonFacade"/>, then shows both
/// of its clients in the game, <see cref="World"/> and <see cref="LootDropper"/>, going through it.
/// </summary>
public static class FacadeDemo
{
    public static void Run()
    {
        Console.WriteLine("FACADE: DungeonFacade is the one entry point to the dungeon subsystems");
        Console.WriteLine();
        Console.WriteLine("  Subsystems  FloorDirector, StandardFloorBuilder, BossRushFloorBuilder,");
        Console.WriteLine("              StandardEnemyFactory / BossRushEnemyFactory, PickupFactory");
        Console.WriteLine("  Facade      DungeonFacade: CreateFloor, SpawnEnemies, DropClearReward");
        Console.WriteLine("  Clients     World, LootDropper");

        var dungeon = new DungeonFacade(new Random(7));
        ShowFloors(dungeon);
        ShowEnemies(dungeon);
        ShowReward(dungeon);
        ShowClients();
    }

    private static void ShowFloors(DungeonFacade dungeon)
    {
        Console.WriteLine();
        Console.WriteLine("== CreateFloor(n): picks the builder and runs the director ==");
        for (int n = 1; n <= Config.BossRushEvery; n++)
        {
            var floor = dungeon.CreateFloor(n);
            Console.WriteLine(
                $"  CreateFloor({n})  {floor.Title,-20} {floor.Rooms.Count,2} rooms" +
                $"  enemies from {floor.EnemyFactory.GetType().Name}");
        }
    }

    private static void ShowEnemies(DungeonFacade dungeon)
    {
        Console.WriteLine();
        Console.WriteLine("== SpawnEnemies(floor, room): uses the floor's own enemy family ==");
        var first = dungeon.CreateFloor(1);
        ShowRoom(dungeon, first, first.Rooms.Values.MaxBy(r => r.Kind == RoomKind.Normal ? r.Spawns.Count : -1)!);
        ShowRoom(dungeon, first, first.Rooms.Values.First(r => r.Kind == RoomKind.Boss));
        var bossRush = dungeon.CreateFloor(Config.BossRushEvery);
        ShowRoom(dungeon, bossRush, bossRush.Rooms.Values.First(r => r.Kind == RoomKind.Boss));
    }

    private static void ShowRoom(DungeonFacade dungeon, Floor floor, Room room)
    {
        var enemies = dungeon.SpawnEnemies(floor, room).Select(e => e.GetType().Name);
        Console.WriteLine($"  {floor.Title,-20} {room.Kind.ToString().ToLowerInvariant(),-6} room -> {string.Join(", ", enemies)}");
    }

    private static void ShowReward(DungeonFacade dungeon)
    {
        Console.WriteLine();
        Console.WriteLine("== DropClearReward(floor, room): asks PickupFactory for the right pickups ==");
        var floor = dungeon.CreateFloor(1);
        var boss = ClearBossRooms(floor);
        dungeon.DropClearReward(floor, boss);
        Console.WriteLine($"  last boss cleared -> {string.Join(", ", boss.Pickups.Select(p => p.GetType().Name))}");
    }

    private static void ShowClients()
    {
        Console.WriteLine();
        Console.WriteLine("== The clients in the game ==");
        var world = new World();
        Console.WriteLine(
            $"  World        StartFloor -> CreateFloor(1): {world.Floor.Title}, {world.Floor.Rooms.Count} rooms;" +
            $" EnterRoom -> SpawnEnemies: {world.Enemies.Count} enemies in the start room");

        var events = new EventBus();
        var dungeon = new DungeonFacade();
        _ = new LootDropper(events, dungeon);
        var floor = dungeon.CreateFloor(1);
        var boss = ClearBossRooms(floor);
        events.Publish(new RoomCleared(boss, floor));
        Console.WriteLine(
            $"  LootDropper  on RoomCleared -> DropClearReward: {string.Join(", ", boss.Pickups.Select(p => p.GetType().Name))}");
    }

    private static Room ClearBossRooms(Floor floor)
    {
        var bosses = floor.Rooms.Values.Where(r => r.Kind == RoomKind.Boss).ToList();
        foreach (var room in bosses)
            room.Cleared = true;
        return bosses[^1];
    }
}
