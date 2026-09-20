namespace Unhallowed.Contracts.Messages;

/// <summary>Sent by a client that wants a seat in a match.</summary>
/// <param name="MatchCode">Lobby code; players sharing a code share a match.</param>
/// <param name="DisplayName">Name shown above the player's sprite.</param>
public sealed record JoinMatchRequest(string MatchCode, string DisplayName);

/// <summary>
/// A single serialized player intent.
/// This is the wire form of the <b>Command</b> pattern (see docs/patterns/14-command.md):
/// the client never mutates the world, it only ships commands the server chooses to execute.
/// </summary>
/// <param name="Sequence">Monotonic per-client counter, used to drop duplicates and reorder.</param>
/// <param name="Type">Discriminator naming the concrete command, e.g. <c>Move</c>, <c>Shoot</c>.</param>
/// <param name="X">Primary axis argument (movement / aim direction), range -1..1.</param>
/// <param name="Y">Secondary axis argument (movement / aim direction), range -1..1.</param>
public sealed record PlayerCommandDto(int Sequence, string Type, float X, float Y);
