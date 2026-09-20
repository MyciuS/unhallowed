namespace Unhallowed.Contracts;

/// <summary>
/// Wire protocol constants shared by <c>Unhallowed.Server</c> and <c>Unhallowed.Client</c>.
/// Requirement 6: client-server over SignalR (WebSockets) with JSON payloads.
/// </summary>
public static class Protocol
{
    /// <summary>Relative path the SignalR hub is mapped to on the server.</summary>
    public const string HubPath = "/hub/match";

    /// <summary>Authoritative simulation rate on the server, in ticks per second.</summary>
    public const int TickRate = 30;

    /// <summary>Rate at which the server broadcasts world snapshots, in snapshots per second.</summary>
    public const int SnapshotRate = 15;

    /// <summary>Hard cap on concurrent players in a single match.</summary>
    public const int MaxPlayers = 4;

    /// <summary>Method names the <i>server</i> invokes on connected clients.</summary>
    public static class ServerToClient
    {
        public const string MatchJoined = nameof(MatchJoined);
        public const string PlayerJoined = nameof(PlayerJoined);
        public const string PlayerLeft = nameof(PlayerLeft);
        public const string Snapshot = nameof(Snapshot);
        public const string RoomChanged = nameof(RoomChanged);
        public const string GameEvent = nameof(GameEvent);
    }

    /// <summary>Method names <i>clients</i> invoke on the server hub.</summary>
    public static class ClientToServer
    {
        public const string JoinMatch = nameof(JoinMatch);
        public const string LeaveMatch = nameof(LeaveMatch);
        public const string SendCommand = nameof(SendCommand);
    }
}
