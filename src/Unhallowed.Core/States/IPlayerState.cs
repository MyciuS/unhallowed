using Unhallowed.Core.Entities;

namespace Unhallowed.Core.States;

// PATTERN: State -> docs/patterns/20-state.md
// The player behaves differently while idle, moving, hurt (invulnerable) or dead. Each of those
// is a class; Player.Update delegates instead of running a switch over a status enum.

/// <summary>One behavioural mode of a player character.</summary>
public interface IPlayerState
{
    string Name { get; }

    /// <summary>Whether damage is ignored while in this state (i-frames, death).</summary>
    bool IsInvulnerable { get; }

    /// <summary>Whether player input is allowed to move the character.</summary>
    bool AcceptsInput { get; }

    void Enter(Player player);

    /// <summary>Runs one tick and returns the state to be in next tick (may be itself).</summary>
    IPlayerState Update(Player player, float deltaSeconds, IWorldView world);

    void Exit(Player player);
}
