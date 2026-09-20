using Microsoft.AspNetCore.SignalR;
using Unhallowed.Contracts;
using Unhallowed.Contracts.Messages;
using Unhallowed.Core.Events;
using Unhallowed.Server.Hubs;

namespace Unhallowed.Server.Matches;

// PATTERN: Observer (concrete observer) -> docs/patterns/19-observer.md
// The simulation has no idea SignalR exists. This observer subscribes to the match's EventBus
// and relays each gameplay event to the group. Swap it for a logger or a stats collector and
// nothing in Core changes.

/// <summary>Relays <see cref="GameEvent"/>s from one match's bus out to that match's clients.</summary>
public sealed class SignalREventBroadcaster(
    IHubContext<MatchHub> hub,
    string matchCode) : IGameEventObserver
{
    public void OnNotify(GameEvent gameEvent)
    {
        // Fire-and-forget: the simulation tick must never block on the network.
        _ = hub.Clients.Group(matchCode).SendAsync(
            Protocol.ServerToClient.GameEvent,
            new GameEventDto(gameEvent.Type.ToString(), gameEvent.Message));
    }
}
