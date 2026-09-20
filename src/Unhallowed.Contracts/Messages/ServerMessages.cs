namespace Unhallowed.Contracts.Messages;

/// <summary>Acknowledges a successful join and tells the client which player it controls.</summary>
public sealed record MatchJoinedResponse(string MatchCode, int PlayerId, int Seat);

/// <summary>Announces another player entering or leaving the match.</summary>
public sealed record PlayerPresenceDto(int PlayerId, string DisplayName, int Seat);

/// <summary>One player as the server sees it this tick. Flat and primitive-only to keep JSON small.</summary>
public sealed record PlayerSnapshotDto(int Id, string DisplayName, int Seat, float X, float Y);

/// <summary>A full picture of the world at one tick. The client renders these and never simulates.</summary>
public sealed record WorldSnapshotDto(int Tick, IReadOnlyList<PlayerSnapshotDto> Players);
