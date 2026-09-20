namespace Unhallowed.Contracts.Messages;

/// <summary>Sent by a client that wants a seat in a match.</summary>
/// <param name="MatchCode">Lobby code; players sharing a code share a match.</param>
/// <param name="DisplayName">Name shown above the player's dot.</param>
public sealed record JoinMatchRequest(string MatchCode, string DisplayName);

/// <summary>
/// The latest direction a player wants to walk. Clients never move themselves - they send this
/// intent and the server applies it on the next authoritative tick.
/// </summary>
/// <param name="X">Horizontal axis, range -1..1.</param>
/// <param name="Y">Vertical axis, range -1..1.</param>
public sealed record MoveIntent(float X, float Y);
