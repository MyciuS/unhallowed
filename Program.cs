using Isaac;
using Isaac.Demos;
using Isaac.Net;

// Usage:
//   dotnet run                 -> main menu
//   dotnet run -- --host       -> host a co-op game immediately
//   dotnet run -- --join IP    -> join a host (IP or IP:PORT)
//   dotnet run -- --solo       -> singleplayer
//   dotnet run -- --demo NAME  -> console demo of a design pattern (e.g. prototype)
if (args is ["--demo", var demoName, ..])
{
    PatternDemos.Run(demoName);
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
