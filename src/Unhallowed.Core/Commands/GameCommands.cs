using Unhallowed.Core.Common;
using Unhallowed.Core.Entities;

namespace Unhallowed.Core.Commands;

// PATTERN: Command (concrete commands) -> docs/patterns/14-command.md

/// <summary>Sets the player's walk direction. Magnitude is clamped so clients cannot speed-hack.</summary>
public sealed class MoveCommand(int sequence, Vec2 direction) : IGameCommand
{
    public string Type => "Move";

    public int Sequence => sequence;

    public void Execute(Player player, IWorldView world)
    {
        var clamped = direction.Length > 1f ? direction.Normalized() : direction;
        player.MoveIntent = clamped;
    }
}

/// <summary>Sets the player's aim direction. Passing zero stops firing.</summary>
public sealed class ShootCommand(int sequence, Vec2 direction) : IGameCommand
{
    public string Type => "Shoot";

    public int Sequence => sequence;

    public void Execute(Player player, IWorldView world)
    {
        player.AimIntent = direction.Length > 1f ? direction.Normalized() : direction;
    }
}

/// <summary>Does nothing. Returned instead of null when a client sends an unknown command type.</summary>
public sealed class NoOpCommand(int sequence) : IGameCommand
{
    public string Type => "NoOp";

    public int Sequence => sequence;

    public void Execute(Player player, IWorldView world)
    {
    }
}
