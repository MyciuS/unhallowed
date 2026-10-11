using Isaac.Game.Entities;

namespace Isaac.Game.Dungeon;

/// <summary>
/// BUILDER pattern, a Concrete Builder: the boss rush floor. A corridor of boss
/// rooms leading away from the start, with a treasure room beside the start to
/// power up first. No rocks and no regular enemies.
/// </summary>
public sealed class BossRushFloorBuilder(int? seed = null) : IFloorBuilder
{
    private readonly Random _rng = seed is { } s ? new Random(s) : new Random();
    private readonly IEnemyFactory _enemyFactory = new BossRushEnemyFactory();
    private int _number;
    private Dictionary<(int, int), Room> _rooms = FloorLayout.NewGrid();
    private List<Room> _corridor = [];
    private Room? _sideRoom;

    /// <summary>Floor 3 has 3 bosses, floor 6 has 4, and so on up to 5.</summary>
    public int BossCount => Math.Min(2 + _number / 3, 5);

    public void Reset(int floorNumber)
    {
        _number = floorNumber;
        _rooms = FloorLayout.NewGrid();
        _corridor = [];
        _sideRoom = null;
    }

    public void BuildLayout()
    {
        // The corridor can wind into a dead end, so retry until it fits.
        for (int attempt = 0; attempt < 100; attempt++)
        {
            _rooms = FloorLayout.NewGrid();
            _corridor = [_rooms[(0, 0)]];
            for (int guard = 0; _corridor.Count <= BossCount && guard < 200; guard++)
                if (FloorLayout.TryGrow(_rooms, _corridor[^1], _rng) is { } next)
                    _corridor.Add(next);

            _sideRoom = null;
            for (int guard = 0; _sideRoom == null && guard < 50; guard++)
                _sideRoom = FloorLayout.TryGrow(_rooms, _rooms[(0, 0)], _rng);

            if (_corridor.Count == BossCount + 1 && _sideRoom != null) return;
        }
    }

    public void AssignRoomKinds()
    {
        _corridor[0].Kind = RoomKind.Start;
        foreach (var room in _corridor.Skip(1)) room.Kind = RoomKind.Boss;
        if (_sideRoom != null) _sideRoom.Kind = RoomKind.Treasure;
    }

    public void AddObstacles()
    {
        // Boss arenas stay open.
    }

    public void AddEnemies()
    {
        foreach (var room in _rooms.Values.Where(r => r.Kind == RoomKind.Boss))
            room.Spawns.Add(new EnemySpawn(EnemyRole.Boss, FloorLayout.BossSpawn));
    }

    public void AddRewards()
    {
        _sideRoom?.Pickups.Add(FloorLayout.RandomItem(_rng));
    }

    public Floor GetResult() => FloorLayout.Finish(_number, $"Floor {_number}: Boss Rush", _rooms, _enemyFactory);
}
