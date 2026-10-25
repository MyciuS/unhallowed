namespace Isaac.Game.Dungeon;

public enum FloorType
{
    Standard,
    BossRush,
}

/// <summary>
/// Demo mode: start the run on a chosen floor type and number instead of floor 1,
/// so either builder can be shown straight away. Later floors follow the normal rotation.
/// </summary>
public sealed record DemoStart(FloorType Type, int FloorNumber)
{
    public const int MaxFloor = 9;

    public string Label => $"{(Type == FloorType.BossRush ? "Boss Rush" : "Normal")} floor {FloorNumber}";
}
