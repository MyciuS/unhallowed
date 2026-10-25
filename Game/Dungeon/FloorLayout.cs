using System.Numerics;
using Isaac.Game.Entities;

namespace Isaac.Game.Dungeon;

/// <summary>Grid helpers shared by the floor builders.</summary>
internal static class FloorLayout
{
    /// <summary>A new room grid holding only the start room at (0, 0).</summary>
    public static Dictionary<(int, int), Room> NewGrid() => new() { [(0, 0)] = new Room(0, 0) };

    public static int CountNeighbors(Dictionary<(int, int), Room> rooms, int x, int y) =>
        Enum.GetValues<Direction>()
            .Count(d =>
            {
                var (dx, dy) = d.Offset();
                return rooms.ContainsKey((x + dx, y + dy));
            });

    /// <summary>
    /// Tries to add a room next to <paramref name="from"/> in a random direction. Only empty cells
    /// touching exactly one room are used, which keeps the map branchy instead of blobby.
    /// </summary>
    public static Room? TryGrow(Dictionary<(int, int), Room> rooms, Room from, Random rng)
    {
        var (dx, dy) = ((Direction)rng.Next(4)).Offset();
        int x = from.GridX + dx,
            y = from.GridY + dy;
        if (rooms.ContainsKey((x, y)) || CountNeighbors(rooms, x, y) != 1)
            return null;
        var room = new Room(x, y);
        rooms[(x, y)] = room;
        return room;
    }

    public static Pickup RandomItem(Random rng) =>
        PickupFactory.CreateRandomItemPedestal(Room.Center, rng);

    public static Vector2 BossSpawn => Room.Center + new Vector2(0, -60);

    /// <summary>Opens doors between neighbouring rooms and wraps the grid up as a <see cref="Floor"/>.</summary>
    public static Floor Finish(
        int number,
        string title,
        Dictionary<(int, int), Room> rooms,
        IEnemyFactory enemyFactory
    )
    {
        foreach (var room in rooms.Values)
        {
            foreach (var d in Enum.GetValues<Direction>())
            {
                var (dx, dy) = d.Offset();
                room.Doors[(int)d] = rooms.ContainsKey((room.GridX + dx, room.GridY + dy));
            }
            // Rooms without enemies start out cleared, so their doors are open.
            room.Cleared = room.Spawns.Count == 0;
        }
        var start = rooms[(0, 0)];
        start.Visited = true;
        return new Floor(number, title, rooms, start, enemyFactory);
    }
}
