namespace Unhallowed.Core.Events;

/// <summary>Kinds of gameplay notification the simulation can raise.</summary>
public enum GameEventType
{
    EnemyDied,
    PlayerDamaged,
    PlayerDied,
    ItemPickedUp,
    RoomCleared,
    RoomEntered,
    BossSpawned,
}

/// <summary>An immutable notification pushed through the <see cref="EventBus"/>.</summary>
/// <param name="Type">What happened.</param>
/// <param name="SourceEntityId">Entity that caused the event, or 0 when the world raised it.</param>
/// <param name="Message">Human-readable text for the HUD and the server log.</param>
public sealed record GameEvent(GameEventType Type, int SourceEntityId, string Message);
