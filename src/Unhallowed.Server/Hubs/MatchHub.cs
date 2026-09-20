using Microsoft.AspNetCore.SignalR;
using Unhallowed.Contracts;
using Unhallowed.Contracts.Messages;
using Unhallowed.Core.Commands;
using Unhallowed.Server.Matches;

namespace Unhallowed.Server.Hubs;

/// <summary>
/// The single network entry point - requirement 6 (client-server over SignalR, JSON payloads).
/// The hub only validates and forwards; it never runs game logic itself, and it never mutates
/// the world directly. Commands go into a queue that the fixed-step loop drains.
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

    public async Task LeaveMatch()
    {
        await RemoveCallerAsync();
    }

    /// <summary>
    /// Accepts one serialized <b>Command</b> object. Rehydrating it here - rather than acting on
    /// raw floats - is what lets the server queue, reorder and drop client intents safely.
    /// </summary>
    public Task SendCommand(PlayerCommandDto dto)
    {
        var match = matches.FindByConnection(Context.ConnectionId);
        var playerId = match?.PlayerIdFor(Context.ConnectionId);

        if (match is null || playerId is null)
        {
            return Task.CompletedTask;
        }

        match.World.EnqueueCommand(playerId.Value, CommandTranslator.FromDto(dto));
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
