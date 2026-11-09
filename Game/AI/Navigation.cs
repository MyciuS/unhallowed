using System.Numerics;
using Isaac.Core;
using Isaac.Game.Dungeon;

namespace Isaac.Game.AI;

/// <summary>Shortest paths around rocks on a room's tile grid.</summary>
internal static class RoomNavigation
{
    private static readonly (int dx, int dy)[] Neighbors =
        [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)];

    public static (int x, int y) TileOf(Vector2 pos) =>
        (Math.Clamp((int)(pos.X / Config.Tile), 0, Config.RoomTilesW - 1),
         Math.Clamp((int)(pos.Y / Config.Tile), 0, Config.RoomTilesH - 1));

    /// <summary>
    /// Walking distance (in tiles) from <paramref name="from"/> to every tile, moving in 8 directions
    /// without cutting rock corners. Unreachable tiles and rocks are <see cref="float.PositiveInfinity"/>.
    /// </summary>
    public static float[,] Distances(Room room, (int x, int y) from)
    {
        var dist = new float[Config.RoomTilesW, Config.RoomTilesH];
        for (int x = 0; x < Config.RoomTilesW; x++)
        for (int y = 0; y < Config.RoomTilesH; y++)
            dist[x, y] = float.PositiveInfinity;

        var queue = new PriorityQueue<(int x, int y), float>();
        dist[from.x, from.y] = 0;
        queue.Enqueue(from, 0);
        while (queue.TryDequeue(out var tile, out float d))
        {
            if (d > dist[tile.x, tile.y])
                continue;
            foreach (var (dx, dy) in Neighbors)
            {
                int nx = tile.x + dx, ny = tile.y + dy;
                if (!CanStep(room, tile.x, tile.y, dx, dy))
                    continue;
                float nd = d + (dx != 0 && dy != 0 ? MathF.Sqrt(2) : 1);
                if (nd >= dist[nx, ny])
                    continue;
                dist[nx, ny] = nd;
                queue.Enqueue((nx, ny), nd);
            }
        }
        return dist;
    }

    /// <summary>The centre of the neighbouring tile that is closest to the goal the field was built for, or null if none is closer.</summary>
    public static Vector2? NextStep(Room room, float[,] distToGoal, Vector2 pos)
    {
        var (x, y) = TileOf(pos);
        float best = distToGoal[x, y];
        Vector2? step = null;
        foreach (var (dx, dy) in Neighbors)
        {
            if (!CanStep(room, x, y, dx, dy) || distToGoal[x + dx, y + dy] >= best)
                continue;
            best = distToGoal[x + dx, y + dy];
            step = Room.TileCenter(x + dx, y + dy);
        }
        return step;
    }

    private static bool CanStep(Room room, int x, int y, int dx, int dy) =>
        !room.IsSolidTile(x + dx, y + dy)
        // Diagonal steps need both sides free, otherwise the body would clip the rock's corner.
        && (dx == 0 || dy == 0 || (!room.IsSolidTile(x + dx, y) && !room.IsSolidTile(x, y + dy)));
}

/// <summary>Steers towards a goal: straight at it when nothing is in the way, otherwise along the shortest path around rocks.</summary>
internal sealed class PathFollower
{
    private float[,]? _field;
    private (int x, int y) _fieldGoal;
    private Room? _fieldRoom;

    public Vector2 Steer(World world, Vector2 pos, float radius, Vector2 goal)
    {
        if (world.HasClearPath(pos, goal, radius))
            return goal;
        var tile = RoomNavigation.TileOf(goal);
        if (_field == null || tile != _fieldGoal || world.Room != _fieldRoom)
        {
            _field = RoomNavigation.Distances(world.Room, tile);
            _fieldGoal = tile;
            _fieldRoom = world.Room;
        }
        return RoomNavigation.NextStep(world.Room, _field, pos) ?? goal;
    }
}
