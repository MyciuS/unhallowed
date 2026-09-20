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
                await StepAllMatchesAsync();
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

    private async Task StepAllMatchesAsync()
    {
        foreach (var match in matches.ActiveMatches)
        {
            match.World.Step(FixedDelta);

            if (match.World.Tick % _ticksPerSnapshot != 0)
            {
                continue;
            }

            var players = match.World.Players
                .Select(p => new PlayerSnapshotDto(p.Id, p.DisplayName, p.Seat, p.Position.X, p.Position.Y))
                .ToArray();

            var dto = new WorldSnapshotDto(match.World.Tick, players);
            await hub.Clients.Group(match.Code).SendAsync(Protocol.ServerToClient.Snapshot, dto);
        }
    }
}
