using Unhallowed.Core.Common;
using Unhallowed.Core.Events;

namespace Unhallowed.Core.Entities;

/// <summary>
/// The slice of the world an entity is allowed to see while updating.
/// Narrow on purpose: entities can query and spawn, but cannot restructure the world.
/// </summary>
public interface IWorldView
{
    IReadOnlyList<Entity> Entities { get; }

    Vec2 RoomSize { get; }

    IGameEventSubject Events { get; }

    /// <summary>Nearest living player to <paramref name="origin"/>, or null if all are down.</summary>
    Entity? FindNearestPlayer(Vec2 origin);

    void Spawn(Entity entity);

    int NextEntityId();
}
