using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Unhallowed.Contracts;
using Unhallowed.Contracts.Messages;
using Unhallowed.Server.Hubs;

namespace Unhallowed.Server.Matches;

/// <summary>
/// The authoritative fixed-step game loop. Steps every live match at
/// <see cref="Protocol.TickRate"/> Hz and broadcasts a world snapshot at
/// <see cref="Protocol.SnapshotRate"/> Hz.
/// </summary>
public sealed class MatchLoop(
    MatchManager matches,
    IHubContext<MatchHub> hub,
    ILogger<MatchLoop> logger) : BackgroundService
{
    private const float FixedDelta = 1f / Protocol.TickRate;
    private static readonly int _ticksPerSnapshot = Protocol.TickRate / Protocol.SnapshotRate;

    private readonly HashSet<string> _wiredMatches = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Match loop started: {TickRate} Hz simulation, {SnapshotRate} Hz snapshots",
            Protocol.TickRate, Protocol.SnapshotRate);

        var stopwatch = Stopwatch.StartNew();
        var nextTickMs = 0d;
        var tickIntervalMs = 1000d / Protocol.TickRate;

        while (!stoppingToken.IsCancellationRequested)
        {
            nextTickMs += tickIntervalMs;

            try
            {
                StepAllMatches();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Simulation tick failed");
            }

            var delayMs = nextTickMs - stopwatch.Elapsed.TotalMilliseconds;
            if (delayMs > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), stoppingToken);
            }
            else
            {
                // Fell behind: give up the lost time rather than spiral.
                nextTickMs = stopwatch.Elapsed.TotalMilliseconds;
            }
        }
    }

    private void StepAllMatches()
    {
        foreach (var match in matches.ActiveMatches)
        {
            WireEventsOnce(match);

            match.World.Step(FixedDelta);

            var snapshot = match.World.Capture();
            match.History.Push(snapshot);

            if (match.World.Tick % _ticksPerSnapshot != 0)
            {
                continue;
            }

            Broadcast(match, snapshot);
        }
    }

    /// <summary>Attaches the relay observer the first time this loop sees a match.</summary>
    private void WireEventsOnce(Match match)
    {
        if (_wiredMatches.Add(match.Code))
        {
            match.Events.Attach(new SignalREventBroadcaster(hub, match.Code));
        }
    }

    private void Broadcast(Match match, Core.Simulation.WorldSnapshot snapshot)
    {
        var entities = snapshot.Entities
            .Select(e => new EntitySnapshotDto(
                e.Id, e.Kind, e.Position.X, e.Position.Y, e.Radius, e.Health, e.MaxHealth, e.Visual))
            .ToArray();

        // Each client gets its own ack sequence so it can reconcile its prediction.
        foreach (var (connectionId, playerId) in match.Connections)
        {
            var dto = new WorldSnapshotDto(
                snapshot.Tick,
                snapshot.RoomId,
                match.World.AckSequenceFor(playerId),
                entities);

            _ = hub.Clients.Client(connectionId).SendAsync(Protocol.ServerToClient.Snapshot, dto);
        }
    }
}
