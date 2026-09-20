namespace Unhallowed.Contracts;

/// <summary>
/// Wire protocol constants shared by <c>Unhallowed.Server</c> and <c>Unhallowed.Client</c>.
/// Client-server over SignalR (WebSockets) with JSON payloads; the server is authoritative.
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

    /// <summary>Arena width in world units. Shared so client and server agree on the bounds.</summary>
    public const float ArenaWidth = 960f;

    /// <summary>Arena height in world units.</summary>
    public const float ArenaHeight = 540f;

    /// <summary>Player collision radius in world units.</summary>
    public const float PlayerRadius = 12f;

    /// <summary>Method names the <i>server</i> invokes on connected clients.</summary>
    public static class ServerToClient
    {
        public const string PlayerJoined = nameof(PlayerJoined);
        public const string PlayerLeft = nameof(PlayerLeft);
        public const string Snapshot = nameof(Snapshot);
    }

    /// <summary>Method names <i>clients</i> invoke on the server hub.</summary>
    public static class ClientToServer
    {
        public const string JoinMatch = nameof(JoinMatch);
        public const string LeaveMatch = nameof(LeaveMatch);
        public const string SendMove = nameof(SendMove);
    }
}
