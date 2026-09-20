using Microsoft.AspNetCore.SignalR.Client;
using Unhallowed.Contracts;
using Unhallowed.Contracts.Messages;

namespace Unhallowed.Client.Net;

/// <summary>Single entry point the UI uses for everything network-related.</summary>
internal sealed class GameClient : IAsyncDisposable
{
    private readonly SnapshotBuffer _snapshots = new();
    private HubConnection? _connection;

    public int PlayerId { get; private set; }

    public int Seat { get; private set; }

    public string MatchCode { get; private set; } = string.Empty;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public int ServerTick => _snapshots.LatestTick;

    /// <summary>Rolling log of join/leave events, newest last. Drawn in the HUD.</summary>
    public List<string> EventLog { get; } = [];

    /// <summary>Players as they should be drawn right now, interpolated between snapshots.</summary>
    public IReadOnlyList<PlayerSnapshotDto> Players => _snapshots.GetInterpolated();

    public async Task ConnectAsync(string serverUrl, string matchCode, string displayName)
    {
        var hubUrl = serverUrl.TrimEnd('/') + Protocol.HubPath;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<WorldSnapshotDto>(Protocol.ServerToClient.Snapshot, _snapshots.Push);

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

    public async Task SendMoveAsync(float x, float y)
    {
        if (_connection is null || _connection.State != HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            await _connection.SendAsync(Protocol.ClientToServer.SendMove, new MoveIntent(x, y));
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
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
