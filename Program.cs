using Isaac;
using Isaac.Core;
using Isaac.Game.Dungeon;
using Isaac.Net;

// Usage:
//   dotnet run                 -> main menu
//   dotnet run -- --host       -> host a co-op game immediately
//   dotnet run -- --join IP    -> join a host (IP or IP:PORT)
//   dotnet run -- --solo       -> singleplayer
//   dotnet run -- --demo boss 3   -> singleplayer starting on a chosen floor ("normal" or "boss", number 1-9)
//   dotnet run -- --server [PORT] -> headless dedicated server, no window (e.g. on a VPS)
if (args is ["--server", ..])
{
    int port = args.Length > 1 && int.TryParse(args[1], out int p) ? p : Config.DefaultPort;
    DedicatedServer.Run(port);
    return;
}

var game = new IsaacGame();
switch (args)
{
    case ["--host", ..]: game.StartSession(new HostSession(networked: true)); break;
    case ["--join", var address, ..]: game.StartSession(Menu.CreateClient(address)); break;
    case ["--solo", ..]: game.StartSession(new HostSession(networked: false)); break;
    case ["--demo", ..]: game.StartSession(new HostSession(networked: false, demo: ParseDemo(args[1..]))); break;
}
game.Run();

static DemoStart ParseDemo(string[] rest)
{
    var type = rest is [var t, ..] && t.StartsWith("boss", StringComparison.OrdinalIgnoreCase)
        ? FloorType.BossRush
        : FloorType.Standard;
    int floor = rest is [_, var n, ..] && int.TryParse(n, out int f) ? Math.Clamp(f, 1, DemoStart.MaxFloor) : 1;
    return new DemoStart(type, floor);
}
