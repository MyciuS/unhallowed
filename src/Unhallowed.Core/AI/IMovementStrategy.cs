using Unhallowed.Core.Common;
using Unhallowed.Core.Entities;

namespace Unhallowed.Core.AI;

// PATTERN: Strategy -> docs/patterns/21-strategy.md
// Enemy behaviour is swapped at runtime instead of branched on an enemy-type enum. Adding a
// new movement style means adding one class, not editing Enemy.Update.

/// <summary>Decides where an enemy wants to move this tick.</summary>
public interface IMovementStrategy
{
    /// <summary>Display name used by the pattern demos and the debug HUD.</summary>
    string Name { get; }

    /// <summary>Returns the desired velocity for <paramref name="self"/> this tick.</summary>
    Vec2 ComputeVelocity(Entity self, IWorldView world, float deltaSeconds);
}
