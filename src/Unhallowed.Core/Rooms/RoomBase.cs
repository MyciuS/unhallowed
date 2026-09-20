using Unhallowed.Core.Common;
using Unhallowed.Core.Entities;
using Unhallowed.Core.Events;

namespace Unhallowed.Core.Rooms;

// PATTERN: Template Method -> docs/patterns/22-template-method.md
// Every room type follows the same lifecycle - seal the doors, populate, wait for a clear,
// grant a reward, open the doors. Only the variable steps are left to subclasses.

/// <summary>Base for every room. The lifecycle skeleton lives here and cannot be overridden.</summary>
public abstract class RoomBase
{
    protected RoomBase(string id, Vec2 size)
    {
        Id = id;
        Size = size;
    }

    public string Id { get; }

    public Vec2 Size { get; }

    public bool DoorsSealed { get; private set; }

    public bool Cleared { get; private set; }

    /// <summary>Label sent to the client so the HUD can name the room.</summary>
    public abstract string RoomType { get; }

    /// <summary>
    /// THE TEMPLATE METHOD. Non-virtual on purpose: subclasses customise the steps, never the
    /// order of the steps.
    /// </summary>
    public void Enter(IWorldView world)
    {
        ArgumentNullException.ThrowIfNull(world);

        OnBeforePopulate(world);
        Populate(world);
        DoorsSealed = RequiresClearing;
        Cleared = !RequiresClearing;

        world.Events.Notify(new GameEvent(
            GameEventType.RoomEntered, 0, $"Entered {RoomType} room {Id}"));

        OnAfterPopulate(world);
    }

    /// <summary>
    /// Called every tick by the simulation. Once the room empties out, runs the back half of the
    /// lifecycle exactly once.
    /// </summary>
    public void Tick(IWorldView world)
    {
        if (Cleared || !RequiresClearing)
        {
            return;
        }

        if (world.Entities.Any(e => e is Enemy { IsAlive: true }))
        {
            return;
        }

        Cleared = true;
        DoorsSealed = false;
        GrantReward(world);

        world.Events.Notify(new GameEvent(
            GameEventType.RoomCleared, 0, $"{RoomType} room {Id} cleared"));
    }

    /// <summary>Required step: fill the room with its enemies, props and pickups.</summary>
    protected abstract void Populate(IWorldView world);

    /// <summary>Whether the doors lock until every enemy is dead. Treasure rooms say no.</summary>
    protected virtual bool RequiresClearing => true;

    /// <summary>Optional hook before population - lighting, music, boss intro.</summary>
    protected virtual void OnBeforePopulate(IWorldView world)
    {
    }

    /// <summary>Optional hook after population.</summary>
    protected virtual void OnAfterPopulate(IWorldView world)
    {
    }

    /// <summary>Optional step: what drops when the room is cleared. Default is nothing.</summary>
    protected virtual void GrantReward(IWorldView world)
    {
    }
}
