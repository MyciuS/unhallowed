namespace Isaac.Game.Dungeon;

/// <summary>
/// BUILDER pattern, the Director: knows the order of the construction steps,
/// but not how any floor type is actually built. The same recipe works with
/// every <see cref="IFloorBuilder"/>.
/// </summary>
public sealed class FloorDirector
{
    public Floor Construct(IFloorBuilder builder, int floorNumber)
    {
        builder.Reset(floorNumber);
        builder.BuildLayout();
        builder.AssignRoomKinds();
        builder.AddObstacles();
        builder.AddEnemies();
        builder.AddRewards();
        return builder.GetResult();
    }
}
