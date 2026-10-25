using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Isaac.Core;
using Isaac.Game;
using Microsoft.AspNetCore.SignalR;

namespace Isaac.Net;

/// <summary>Names of the methods the host invokes on clients.</summary>
public static class ClientMethods
{
    public const string Welcome = "Welcome"; // byte: your player id
    public const string Snapshot = "Snapshot"; // Snapshot
    public const string Rejected = "Rejected"; // string: reason
}

/// <summary>
/// JSON settings shared by the host and the clients. Both sides must use the same ones,
/// otherwise they can't read each other's messages.
/// </summary>
public static class NetJson
{
    public static void Configure(JsonSerializerOptions options)
    {
        // Snapshot and its views are plain structs with public fields, not properties.
        options.IncludeFields = true;
        // Keep snapshots small: enums stay numbers, and false/0 fields are left out
        // (the reader fills in the same defaults).
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    }
}

public abstract record HubEvent(string ConnectionId);

public sealed record ClientJoined(string ConnectionId) : HubEvent(ConnectionId);

public sealed record ClientLeft(string ConnectionId) : HubEvent(ConnectionId);

public sealed record ClientInput(string ConnectionId, PlayerInput Input) : HubEvent(ConnectionId);

public sealed record ClientAck(string ConnectionId) : HubEvent(ConnectionId);

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

    /// <summary>Called by clients whenever their <see cref="PlayerInput"/> changes.</summary>
    public void Input(PlayerInput input) =>
        inbox.Events.Enqueue(new ClientInput(Context.ConnectionId, input));

    /// <summary>Called by clients for every snapshot they receive.</summary>
    public void Ack() => inbox.Events.Enqueue(new ClientAck(Context.ConnectionId));
}
