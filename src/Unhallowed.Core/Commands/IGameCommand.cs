using Unhallowed.Core.Entities;

namespace Unhallowed.Core.Commands;

// PATTERN: Command -> docs/patterns/14-command.md
// Every player intent is an object. That buys three things at once:
//   1. the client can serialise intents to JSON and ship them over SignalR (requirement 6),
//   2. the server can queue, validate, replay or drop them before they touch the world,
//   3. a recorded queue replays a whole match for debugging.

/// <summary>A single reified player intent that the server may choose to execute.</summary>
public interface IGameCommand
{
    /// <summary>Discriminator used on the wire.</summary>
    string Type { get; }

    /// <summary>Per-client ordering counter; the server ignores stale sequences.</summary>
    int Sequence { get; }

    void Execute(Player player, IWorldView world);
}
