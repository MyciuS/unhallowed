using Isaac.Core;
using Isaac.Game;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Isaac.Net;

/// <summary>
/// The game loop talks to the game only through this interface, so it does
/// not care whether it is offline, hosting, or connected to a remote host.
/// </summary>
public interface IGameSession : IDisposable
{
    bool IsHost { get; }
    int LocalPlayerId { get; }
    Snapshot? Latest { get; }
    string Status { get; }
    string? Error { get; }
    void Update(float dt, PlayerInput localInput);
    void RequestRestart();
}

/// <summary>
/// Runs the authoritative World. With networking enabled it also hosts a SignalR
/// hub that accepts up to three remote players, applies their commands and
/// broadcasts snapshots.
/// </summary>
public sealed class HostSession : IGameSession
{
    private const int LocalId = 0;

    private sealed class RemotePlayer(int id)
    {
        public int Id { get; } = id;
        // Snapshots sent to this client that it has not acknowledged yet.
        public int InFlight { get; set; }
    }

    private readonly World _world = new();
    private readonly WebApplication? _server;
    private readonly IHubContext<GameHub>? _hub;
    private readonly HubInbox _inbox = new();
    private readonly Dictionary<string, RemotePlayer> _remotes = new();
    private readonly bool _dedicated;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;
    private float _accumulator;
    private volatile Snapshot? _latest;

    /// <param name="dedicated">
    /// Headless server: no local player, so every slot goes to a remote player
    /// and the game restarts by itself after a game over.
    /// </param>
    public HostSession(bool networked, int port = Config.DefaultPort, bool dedicated = false)
    {
        _dedicated = dedicated;
        if (!dedicated) _world.AddPlayer(LocalId);
        Latest = _world.BuildSnapshot();
        if (!networked)
        {
            Status = "Singleplayer";
            return;
        }

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new GameLoggerProvider(GameLogger.Instance));
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        // A busy port is reported through Error; skip the hosting stack trace in the console.
        builder.Logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.None);
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        builder.Services.AddSingleton(_inbox);
        builder.Services
            .AddSignalR(o =>
            {
                o.KeepAliveInterval = Config.KeepAliveInterval;
                o.ClientTimeoutInterval = Config.DisconnectTimeout;
            })
            .AddJsonProtocol(o => NetJson.Configure(o.PayloadSerializerOptions));
        var server = builder.Build();
        server.MapHub<GameHub>(GameHub.Path);
        server.MapGet("/", DescribeStatus);
        try
        {
            server.Start();
            _server = server;
            _hub = server.Services.GetRequiredService<IHubContext<GameHub>>();
            Status = $"Hosting on port {port}";
            GameLogger.Instance.Log(Status);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            ((IDisposable)server).Dispose();
            Error = $"Could not open TCP port {port} (already in use?)";
            GameLogger.Instance.Log(Error);
        }
    }

    public bool IsHost => true;
    public int LocalPlayerId => LocalId;

    /// <summary>The authoritative game, for the host's own developer console.</summary>
    public World World => _world;
    public Snapshot? Latest
    {
        get => _latest;
        private set => _latest = value;
    }

    public string Status { get; } = "";
    public string? Error { get; }
    public int RemotePlayers => _remotes.Count;

    private void Handle(HubEvent e)
    {
        switch (e)
        {
            case ClientJoined:
                if (_world.Players.Count >= Config.MaxPlayers)
                {
                    _hub!.Clients.Client(e.ConnectionId).SendAsync(ClientMethods.Rejected, "Game is full");
                    GameLogger.Instance.Log("Rejected a player: game is full");
                    break;
                }
                int id = Enumerable.Range(0, Config.MaxPlayers).First(i => !_world.Players.ContainsKey(i));
                _remotes[e.ConnectionId] = new RemotePlayer(id);
                _world.AddPlayer(id);
                _hub!.Clients.Client(e.ConnectionId).SendAsync(ClientMethods.Welcome, (byte)id);
                _world.ShowMessage($"P{id + 1} joined");
                GameLogger.Instance.Log($"P{id + 1} joined");
                break;

            case ClientLeft:
                if (!_remotes.Remove(e.ConnectionId, out var left)) break;
                _world.RemovePlayer(left.Id);
                _world.ShowMessage($"P{left.Id + 1} left");
                GameLogger.Instance.Log($"P{left.Id + 1} left");
                // Nobody left on the dedicated server: the next group starts a fresh run.
                if (_dedicated && _remotes.Count == 0) _world.Restart();
                break;

            case ClientInput input:
                if (!_remotes.TryGetValue(e.ConnectionId, out var remote) ||
                    !_world.Players.TryGetValue(remote.Id, out var player)) break;
                input.Input.Sanitized().ApplyTo(player);
                break;

            case ClientAck:
                if (_remotes.TryGetValue(e.ConnectionId, out var acked) && acked.InFlight > 0)
                    acked.InFlight--;
                break;
        }
    }

    public void Update(float dt, PlayerInput localInput)
    {
        while (_inbox.Events.TryDequeue(out var e)) Handle(e);

        if (_dedicated)
            UpdateAutoRestart(dt);
        else if (_world.Players.TryGetValue(LocalId, out var local))
            localInput.ApplyTo(local);

        // Fixed timestep simulation, independent of the render frame rate.
        _accumulator = MathF.Min(_accumulator + dt, Config.TickDt * 5);
        bool ticked = false;
        while (_accumulator >= Config.TickDt)
        {
            _world.Update(Config.TickDt);
            _accumulator -= Config.TickDt;
            ticked = true;
        }
        if (!ticked) return;

        Latest = _world.BuildSnapshot();
        if (_hub == null || _remotes.Count == 0) return;

        foreach (var (connectionId, remote) in _remotes)
        {
            // TCP never drops anything, and SendAsync finishes as soon as the message is buffered,
            // not when it arrives. Without a limit a slow link fills the buffers with stale snapshots
            // and the client falls further and further behind. Waiting for acks keeps it current;
            // snapshots skipped here are simply never sent, the client gets the newest one instead.
            if (remote.InFlight >= Config.MaxSnapshotsInFlight) continue;
            remote.InFlight++;
            _ = _hub.Clients.Client(connectionId).SendAsync(ClientMethods.Snapshot, Latest);
        }
    }

    private const float AutoRestartDelay = 5f;
    private float _gameOverTimer;

    // Clients can't press R, so the dedicated server restarts on its own after a short pause.
    private void UpdateAutoRestart(float dt)
    {
        if (!_world.GameOver)
        {
            _gameOverTimer = 0;
            return;
        }
        _gameOverTimer += dt;
        if (_gameOverTimer >= AutoRestartDelay) RequestRestart();
    }

    public void RequestRestart()
    {
        GameLogger.Instance.Log("Game restarted");
        _world.Restart();
    }

    /// <summary>The JSON a browser gets at the server's root address.</summary>
    private object DescribeStatus()
    {
        // Runs on a web server thread while the game thread changes the World, so it reads
        // only the last published snapshot.
        var s = Latest;
        return new
        {
            service = "isaac-coop",
            hub = GameHub.Path,
            mode = _dedicated ? "dedicated" : "player-hosted",
            tickRate = Config.TickRate,
            maxPlayers = Config.MaxPlayers,
            startedAt = _startedAt,
            uptime = (DateTimeOffset.UtcNow - _startedAt).ToString(@"d\.hh\:mm\:ss"),
            playerCount = s?.Players.Count ?? 0,
            game = s == null
                ? null
                : new
                {
                    floor = s.Floor,
                    score = s.Score,
                    room = s.RoomKind.ToString(),
                    gameOver = s.GameOver,
                    players = s.Players.Select(p => new
                    {
                        player = $"P{p.Id + 1}",
                        character = p.Character,
                        health = p.Health,
                        maxHealth = p.MaxHealth,
                        items = p.ItemCount,
                    }),
                },
        };
    }

    public void Dispose()
    {
        if (_server == null) return;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        _server.StopAsync(cts.Token).GetAwaiter().GetResult();
        ((IDisposable)_server).Dispose();
    }
}

/// <summary>Connects to a host's SignalR hub, sends local commands and receives snapshots.</summary>
public sealed class ClientSession : IGameSession
{
    private readonly HubConnection _connection;
    private readonly string _address;
    private Task _pendingSend = Task.CompletedTask;
    private PlayerInput? _sentInput;

    // Written by SignalR callbacks on thread pool threads, read by the game thread.
    private volatile Snapshot? _latest;
    private volatile string? _error;
    private volatile int _localPlayerId = -1;

    public ClientSession(string host, int port = Config.DefaultPort)
    {
        _address = $"{host}:{port}";
        _connection = new HubConnectionBuilder()
            .WithUrl($"http://{_address}{GameHub.Path}?key={Uri.EscapeDataString(Config.ConnectionKey)}")
            .AddJsonProtocol(o => NetJson.Configure(o.PayloadSerializerOptions))
            .Build();
        _connection.ServerTimeout = Config.DisconnectTimeout;
        _connection.KeepAliveInterval = Config.KeepAliveInterval;

        _connection.On<byte>(ClientMethods.Welcome, id =>
        {
            _localPlayerId = id;
            GameLogger.Instance.Log($"Joined {_address} as P{id + 1}");
        });
        // SignalR runs over TCP, so snapshots arrive in order and the newest is always the last one.
        // Ack right away instead of on the next frame, so the host can send the next one sooner.
        _connection.On<Snapshot>(ClientMethods.Snapshot, snapshot =>
        {
            _latest = snapshot;
            _ = _connection.SendAsync(nameof(GameHub.Ack));
        });
        _connection.On<string>(ClientMethods.Rejected, reason =>
        {
            _error = reason;
            GameLogger.Instance.Log($"Rejected by {_address}: {reason}");
        });
        _connection.Closed += ex =>
        {
            _error ??= ex == null ? "Disconnected" : $"Disconnected: {ex.Message}";
            GameLogger.Instance.Log($"Connection to {_address} closed");
            return Task.CompletedTask;
        };
        _connection.StartAsync().ContinueWith(t =>
        {
            if (!t.IsFaulted) return;
            _error = $"Could not connect to {_address}: {t.Exception!.GetBaseException().Message}";
            GameLogger.Instance.Log(_error);
        });
    }

    public bool IsHost => false;
    public int LocalPlayerId => _localPlayerId;
    public Snapshot? Latest => _latest;
    public string? Error => _error;
    public string Status => _connection.State == HubConnectionState.Connected
        ? $"Connected to {_address}"
        : $"Connecting to {_address}...";

    public void Update(float dt, PlayerInput localInput)
    {
        if (_connection.State != HubConnectionState.Connected || _localPlayerId < 0) return;
        // Input is the full key state and the host keeps the last one, so only changes need sending.
        // Skipping a frame while a send is busy loses nothing: the next frame sends the newest state.
        if (localInput == _sentInput || !_pendingSend.IsCompleted) return;

        _sentInput = localInput;
        _pendingSend = _connection.SendAsync(nameof(GameHub.Input), localInput);
    }

    public void RequestRestart()
    {
        // Only the host controls the game flow.
    }

    public void Dispose()
    {
        try
        {
            _connection.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // Already gone; nothing to clean up.
        }
    }
}
