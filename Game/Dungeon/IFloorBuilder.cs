namespace Isaac.Game.Dungeon;

/// <summary>
/// BUILDER pattern, the Builder interface: the steps every kind of floor is built from.
/// Each concrete builder decides what a step means for its floor type, and
/// <see cref="FloorDirector"/> decides the order the steps run in.
/// </summary>
public interface IFloorBuilder
{
    /// <summary>Starts a new, empty floor. Builders are reused, so this clears the previous one.</summary>
    void Reset(int floorNumber);

    /// <summary>Lays out the grid of rooms.</summary>
    void BuildLayout();

    /// <summary>Marks the start, treasure and boss rooms.</summary>
    void AssignRoomKinds();

    /// <summary>Places rocks inside rooms.</summary>
    void AddObstacles();

    /// <summary>Decides which enemies spawn in each room.</summary>
    void AddEnemies();

    /// <summary>Places items and other pickups.</summary>
    void AddRewards();

    /// <summary>Returns the finished product.</summary>
    Floor GetResult();
}
