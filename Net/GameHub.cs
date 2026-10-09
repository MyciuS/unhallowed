using System.Collections.Concurrent;
using Isaac.Core;
using Microsoft.AspNetCore.SignalR;

namespace Isaac.Net;

/// <summary>Names of the methods the host invokes on clients.</summary>
public static class ClientMethods
{
    public const string Welcome = "Welcome";   // byte: your player id
    public const string Snapshot = "Snapshot"; // byte[]: Snapshot.Write output
    public const string Rejected = "Rejected"; // string: reason
}

public abstract record HubEvent(string ConnectionId);
public sealed record ClientJoined(string ConnectionId) : HubEvent(ConnectionId);
public sealed record ClientLeft(string ConnectionId) : HubEvent(ConnectionId);
public sealed record ClientInput(string ConnectionId, byte[] Data) : HubEvent(ConnectionId);

/// <summary>
/// Hub methods run on thread pool threads, but the World is not thread safe.
/// The hub only queues events here and the HostSession drains them on the game thread.
/// </summary>
public sealed class HubInbox
{
    public ConcurrentQueue<HubEvent> Events { get; } = new();
}

/// <summary>SignalR endpoint that remote players connect to.</summary>
public sealed class GameHub(HubInbox inbox) : Hub
{
    public const string Path = "/game";

    public override Task OnConnectedAsync()
    {
        // Same purpose as the old LiteNetLib connection key: reject other apps and old versions.
        if (Context.GetHttpContext()?.Request.Query["key"].ToString() != Config.ConnectionKey)
            Context.Abort();
        else
            inbox.Events.Enqueue(new ClientJoined(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        inbox.Events.Enqueue(new ClientLeft(Context.ConnectionId));
        return Task.CompletedTask;
    }

    /// <summary>Called by clients every frame with their serialized <see cref="Isaac.Game.PlayerInput"/>.</summary>
    public void Input(byte[] data) => inbox.Events.Enqueue(new ClientInput(Context.ConnectionId, data));
}
