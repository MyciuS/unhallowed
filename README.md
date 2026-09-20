# Unhallowed — multiplayer movement demo

A minimal slice of the Unhallowed project: an authoritative SignalR server and a WinForms/GDI+
client where up to four players join a shared match and walk around a single arena in real time.

This branch (`feat/multiplayer-movement`) is deliberately small. It is the foundation the rest of
the game — enemies, combat, items, rooms — and the 22 GoF design patterns will be built on later,
one feature per branch. The fuller scaffold (with pattern demos) lives on `feat/demo-scaffold`.

## What it does

- Start one server. Start one or more clients.
- Each client picks a server URL, a match code, and a name, then joins.
- Clients sharing a match code share an arena. Move with **WASD** or the **arrow keys**.
- The server is authoritative: clients send a movement *intent*, the server steps the simulation
  at 30 Hz, and broadcasts a world snapshot at 15 Hz. Clients render the snapshots (interpolated
  so remote players move smoothly) and never simulate anything themselves.

## Projects

```
src/
  Unhallowed.Contracts/   JSON wire messages shared by client and server
  Unhallowed.Core/        the arena simulation (Player, World) — no UI dependencies
  Unhallowed.Server/      ASP.NET Core host, SignalR hub, authoritative game loop
  Unhallowed.Client/      WinForms + GDI+ client
```

## Run it

Start the server (listens on port 5080):

```bash
dotnet run --project src/Unhallowed.Server
```

Then start a client (repeat to get more players in the same match):

```bash
dotnet run --project src/Unhallowed.Client
```

Build everything:

```bash
dotnet build
```
