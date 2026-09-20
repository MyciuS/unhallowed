using Unhallowed.Core.Common;

namespace Unhallowed.Core.Entities;

/// <summary>A player character: a dot that walks around the arena under server control.</summary>
public sealed class Player
{
    public Player(int id, string displayName, int seat, Vec2 position)
    {
        Id = id;
        DisplayName = displayName;
        Seat = seat;
        Position = position;
    }

    public int Id { get; }

    public string DisplayName { get; }

    /// <summary>0-3, decides the colour the client draws this player with.</summary>
    public int Seat { get; }

    public Vec2 Position { get; private set; }

    /// <summary>Direction the player wants to walk, set from the latest move intent.</summary>
    public Vec2 MoveIntent { get; set; }

    /// <summary>Advance one fixed timestep: walk in the intended direction, stay inside the arena.</summary>
    public void Update(float deltaSeconds, float speed, Vec2 arenaSize, float radius)
    {
        Position += MoveIntent.Normalized() * speed * deltaSeconds;
        Position = new Vec2(
            Math.Clamp(Position.X, radius, arenaSize.X - radius),
            Math.Clamp(Position.Y, radius, arenaSize.Y - radius));
    }
}
