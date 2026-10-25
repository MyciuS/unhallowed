using Isaac;
using Isaac.Core;
using Isaac.Net;

// Usage:
//   dotnet run                 -> main menu
//   dotnet run -- --host       -> host a co-op game immediately
//   dotnet run -- --join IP    -> join a host (IP or IP:PORT)
//   dotnet run -- --solo       -> singleplayer
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
}
game.Run();
