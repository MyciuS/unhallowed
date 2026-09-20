using Microsoft.AspNetCore.SignalR.Client;
using Unhallowed.Contracts;
using Unhallowed.Contracts.Messages;

namespace Unhallowed.Client.Net;

// PATTERN: Facade -> docs/patterns/10-facade.md
// Behind this one small class sit HubConnection setup, the JSON protocol, automatic reconnect,
// six handler registrations, the outgoing command sequence counter and the interpolation buffer.
// GameForm needs none of that: it calls Connect / SendMove / SendShoot and reads Entities.

/// <summary>Single simplified entry point the UI uses for everything network-related.</summary>
internal sealed class GameClient : IAsyncDisposable
{
    private readonly SnapshotBuffer _snapshots = new();
    private HubConnection? _connection;
    private int _sequence;

    public int PlayerId { get; private set; }

    public int Seat { get; private set; }

    public string MatchCode { get; private set; } = string.Empty;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public string RoomId => _snapshots.RoomId;

    public int ServerTick => _snapshots.LatestTick;

    /// <summary>Rolling log of gameplay events, newest last. Drawn in the HUD.</summary>
    public List<string> EventLog { get; } = [];

    public event Action? StateChanged;

    public IReadOnlyList<EntitySnapshotDto> Entities => _snapshots.GetInterpolated();

    public async Task ConnectAsync(string serverUrl, string matchCode, string displayName)
    {
        var hubUrl = serverUrl.TrimEnd('/') + Protocol.HubPath;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<WorldSnapshotDto>(Protocol.ServerToClient.Snapshot, snapshot =>
        {
            _snapshots.Push(snapshot);
        });

        _connection.On<GameEventDto>(Protocol.ServerToClient.GameEvent, gameEvent =>
        {
            Log(gameEvent.Message);
        });

        _connection.On<PlayerPresenceDto>(Protocol.ServerToClient.PlayerJoined, presence =>
        {
            Log($"{presence.DisplayName} joined (seat {presence.Seat + 1})");
        });

        _connection.On<PlayerPresenceDto>(Protocol.ServerToClient.PlayerLeft, presence =>
        {
            Log($"Player {presence.PlayerId} left");
        });

        await _connection.StartAsync();

        var response = await _connection.InvokeAsync<MatchJoinedResponse>(
            Protocol.ClientToServer.JoinMatch,
            new JoinMatchRequest(matchCode, displayName));

        PlayerId = response.PlayerId;
        Seat = response.Seat;
        MatchCode = response.MatchCode;
        Log($"Joined '{response.MatchCode}' as player {response.PlayerId}, seat {response.Seat + 1}");
    }

    public Task SendMoveAsync(float x, float y) => SendAsync("Move", x, y);

    public Task SendShootAsync(float x, float y) => SendAsync("Shoot", x, y);

    private async Task SendAsync(string type, float x, float y)
    {
        if (_connection is null || _connection.State != HubConnectionState.Connected)
        {
            return;
        }

        var dto = new PlayerCommandDto(Interlocked.Increment(ref _sequence), type, x, y);

        try
        {
            await _connection.SendAsync(Protocol.ClientToServer.SendCommand, dto);
        }
        catch (Exception ex)
        {
            Log($"send failed: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        lock (EventLog)
        {
            EventLog.Add(message);
            if (EventLog.Count > 8)
            {
                EventLog.RemoveAt(0);
            }
        }

        StateChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
