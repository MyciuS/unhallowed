namespace Unhallowed.Contracts.Messages;

/// <summary>Acknowledges a successful join and tells the client which entity it controls.</summary>
public sealed record MatchJoinedResponse(string MatchCode, int PlayerId, int Seat, string RoomId);

/// <summary>Announces another player entering or leaving the match.</summary>
public sealed record PlayerPresenceDto(int PlayerId, string DisplayName, int Seat);

/// <summary>
/// One entity as the server sees it this tick. Intentionally flat and primitive-only so the
/// JSON stays small at <see cref="Protocol.SnapshotRate"/> messages per second.
/// </summary>
public sealed record EntitySnapshotDto(
    int Id,
    string Kind,
    float X,
    float Y,
    float Radius,
    int Health,
    int MaxHealth,
    string Visual);

/// <summary>
/// A full picture of the world at one tick. This is the serialized form of the <b>Memento</b>
/// produced by the simulation (see docs/patterns/18-memento.md) - the client renders snapshots
/// and never runs authoritative logic of its own.
/// </summary>
public sealed record WorldSnapshotDto(
    int Tick,
    string RoomId,
    int AckSequence,
    IReadOnlyList<EntitySnapshotDto> Entities);

/// <summary>A gameplay notification worth surfacing in the HUD or log.</summary>
public sealed record GameEventDto(string Type, string Message);
