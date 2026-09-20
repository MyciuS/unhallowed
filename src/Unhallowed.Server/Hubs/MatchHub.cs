using Microsoft.AspNetCore.SignalR;
using Unhallowed.Contracts;
using Unhallowed.Contracts.Messages;
using Unhallowed.Core.Common;
using Unhallowed.Server.Matches;

namespace Unhallowed.Server.Hubs;

/// <summary>
/// The single network entry point: client-server over SignalR with JSON payloads. The hub only
/// validates and forwards; it never runs game logic itself and never moves a player directly.
/// </summary>
public sealed class MatchHub(MatchManager matches, ILogger<MatchHub> logger) : Hub
{
    public async Task<MatchJoinedResponse> JoinMatch(JoinMatchRequest request)
    {
        var code = string.IsNullOrWhiteSpace(request.MatchCode) ? "default" : request.MatchCode.Trim();
        var name = string.IsNullOrWhiteSpace(request.DisplayName) ? "Player" : request.DisplayName.Trim();

        var match = matches.GetOrCreate(code);
        if (match.IsFull)
        {
            throw new HubException($"Match '{code}' already has {Protocol.MaxPlayers} players.");
        }

        var response = match.Join(Context.ConnectionId, name);
        await Groups.AddToGroupAsync(Context.ConnectionId, code);

        logger.LogInformation(
            "{Name} joined match {Code} as player {PlayerId} (seat {Seat})",
            name, code, response.PlayerId, response.Seat);

        await Clients.OthersInGroup(code).SendAsync(
            Protocol.ServerToClient.PlayerJoined,
            new PlayerPresenceDto(response.PlayerId, name, response.Seat));

        return response;
    }

    public Task LeaveMatch() => RemoveCallerAsync();

    /// <summary>Records the caller's latest movement direction for the next authoritative tick.</summary>
    public Task SendMove(MoveIntent intent)
    {
        var match = matches.FindByConnection(Context.ConnectionId);
        var playerId = match?.PlayerIdFor(Context.ConnectionId);

        if (match is null || playerId is null)
        {
            return Task.CompletedTask;
        }

        match.World.SetMoveIntent(playerId.Value, new Vec2(intent.X, intent.Y));
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await RemoveCallerAsync();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task RemoveCallerAsync()
    {
        var match = matches.FindByConnection(Context.ConnectionId);
        if (match is null)
        {
            return;
        }

        var playerId = match.Leave(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, match.Code);

        if (playerId is not null)
        {
            await Clients.Group(match.Code).SendAsync(
                Protocol.ServerToClient.PlayerLeft,
                new PlayerPresenceDto(playerId.Value, string.Empty, 0));

            logger.LogInformation("Player {PlayerId} left match {Code}", playerId, match.Code);
        }

        matches.RemoveIfEmpty(match.Code);
    }
}
