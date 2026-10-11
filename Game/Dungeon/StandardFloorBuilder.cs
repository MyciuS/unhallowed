using Isaac.Core;
using Isaac.Game.Entities;

namespace Isaac.Game.Dungeon;

/// <summary>
/// BUILDER pattern, a Concrete Builder: the normal floor. Random branching rooms
/// with rocks and regular enemies, a treasure room, and a boss at the far end.
/// </summary>
public sealed class StandardFloorBuilder(int? seed = null) : IFloorBuilder
{
    // Rock layouts. The middle row and column are always kept free so every door is reachable.
    private static readonly (int x, int y)[][] Layouts =
    [
        [],
        [(2, 1), (10, 1), (2, 5), (10, 5)],
        [(3, 2), (3, 4), (9, 2), (9, 4)],
        [
            (1, 1),
            (2, 1),
            (1, 2),
            (11, 1),
            (10, 1),
            (11, 2),
            (1, 5),
            (2, 5),
            (1, 4),
            (11, 5),
            (10, 5),
            (11, 4),
        ],
        [(4, 1), (4, 2), (8, 1), (8, 2), (4, 4), (4, 5), (8, 4), (8, 5)],
        [(2, 2), (4, 2), (8, 2), (10, 2), (2, 4), (4, 4), (8, 4), (10, 4)],
    ];

    private readonly Random _rng = seed is { } s ? new Random(s) : new Random();
    private readonly IEnemyFactory _enemyFactory = new StandardEnemyFactory();
    private int _number;
    private Dictionary<(int, int), Room> _rooms = FloorLayout.NewGrid();

    public void Reset(int floorNumber)
    {
        _number = floorNumber;
        _rooms = FloorLayout.NewGrid();
    }

    public void BuildLayout()
    {
        int roomCount = 7 + _number * 2;
        // Retry until the layout has dead ends for both the boss and the treasure room.
        for (int attempt = 0; attempt < 50; attempt++)
        {
            _rooms = FloorLayout.NewGrid();
            var grown = new List<Room> { _rooms[(0, 0)] };
            for (int guard = 0; _rooms.Count < roomCount && guard < 10_000; guard++)
                if (FloorLayout.TryGrow(_rooms, grown[_rng.Next(grown.Count)], _rng) is { } room)
                    grown.Add(room);
            if (DeadEnds().Count >= 2)
                return;
        }
    }

    public void AssignRoomKinds()
    {
        _rooms[(0, 0)].Kind = RoomKind.Start;
        var deadEnds = DeadEnds();
        if (deadEnds.Count > 0)
        {
            // The boss goes in the dead end furthest from the start.
            deadEnds[0].Kind = RoomKind.Boss;
            deadEnds.RemoveAt(0);
        }
        if (deadEnds.Count > 0)
            deadEnds[_rng.Next(deadEnds.Count)].Kind = RoomKind.Treasure;
    }

    public void AddObstacles()
    {
        foreach (var room in _rooms.Values.Where(r => r.Kind == RoomKind.Normal))
        foreach (var (x, y) in Layouts[_rng.Next(Layouts.Length)])
            room.Rocks[x, y] = true;
    }

    public void AddEnemies()
    {
        foreach (var room in _rooms.Values)
        {
            if (room.Kind == RoomKind.Boss)
                room.Spawns.Add(new EnemySpawn(EnemyRole.Boss, FloorLayout.BossSpawn));
            else if (room.Kind == RoomKind.Normal)
                AddRegularEnemies(room);
        }
    }

    public void AddRewards()
    {
        foreach (var room in _rooms.Values.Where(r => r.Kind == RoomKind.Treasure))
            room.Pickups.Add(FloorLayout.RandomItem(_rng));
    }

    public Floor GetResult() => FloorLayout.Finish(_number, $"Floor {_number}", _rooms, _enemyFactory);

    /// <summary>Rooms with a single door, furthest from the start first.</summary>
    private List<Room> DeadEnds() =>
        _rooms
            .Values.Where(r =>
                !(r.GridX == 0 && r.GridY == 0)
                && FloorLayout.CountNeighbors(_rooms, r.GridX, r.GridY) == 1
            )
            .OrderByDescending(r => Math.Abs(r.GridX) + Math.Abs(r.GridY))
            .ToList();

    private void AddRegularEnemies(Room room)
    {
        int count = 2 + Math.Min(_number, 4) + _rng.Next(3);
        var free = new List<(int x, int y)>();
        for (int x = 1; x < Config.RoomTilesW - 1; x++)
        for (int y = 0; y < Config.RoomTilesH; y++)
        {
            bool nearDoorX =
                Math.Abs(x - Config.RoomTilesW / 2) <= 1 && (y <= 1 || y >= Config.RoomTilesH - 2);
            bool nearDoorY =
                Math.Abs(y - Config.RoomTilesH / 2) <= 1 && (x <= 2 || x >= Config.RoomTilesW - 3);
            if (!room.Rocks[x, y] && !nearDoorX && !nearDoorY)
                free.Add((x, y));
        }
        for (int i = 0; i < count && free.Count > 0; i++)
        {
            int idx = _rng.Next(free.Count);
            var (x, y) = free[idx];
            free.RemoveAt(idx);
            var roles = _enemyFactory.RegularRoles;
            var role = roles[_rng.Next(roles.Count)];
            room.Spawns.Add(new EnemySpawn(role, Room.TileCenter(x, y)));
        }
    }
}
