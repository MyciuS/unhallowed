using Unhallowed.Contracts;
using Unhallowed.Server.Hubs;
using Unhallowed.Server.Matches;

var builder = WebApplication.CreateBuilder(args);

// Client-server over SignalR (WebSockets), JSON payloads.
builder.Services
    .AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddSingleton<MatchManager>();
builder.Services.AddHostedService<MatchLoop>();

var app = builder.Build();

app.MapHub<MatchHub>(Protocol.HubPath);

// Tiny status endpoint so you can confirm the server is up from a browser.
app.MapGet("/", (MatchManager matches) => Results.Json(new
{
    service = "unhallowed-server",
    hub = Protocol.HubPath,
    tickRate = Protocol.TickRate,
    snapshotRate = Protocol.SnapshotRate,
    maxPlayers = Protocol.MaxPlayers,
    activeMatches = matches.ActiveMatches.Select(m => new
    {
        code = m.Code,
        tick = m.World.Tick,
        players = m.World.Players.Count,
    }),
}));

app.Run();
