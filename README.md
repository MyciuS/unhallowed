# unhallowed

A 2D multiplayer roguelike inspired by The Binding of Isaac, featuring chaotic combat,
procedurally generated dungeons, and 22 software design patterns.

Up to 4 players, server-authoritative, written in C# with no game engine.

## Stack

| Layer | Choice | Why |
| --- | --- | --- |
| Language | C# / .NET 10 | Team decision |
| Client | WinForms + GDI+ (`System.Drawing`) | Course requires primitive graphics; no engine allowed |
| Networking | SignalR over WebSockets, JSON | Course requires client-server via WebSockets / SignalR / REST |
| Server | ASP.NET Core, authoritative 30 Hz loop | Clients send intents, never state |
| Database | SQLite via raw ADO.NET | Course credits storing map elements and enemies |

No Unity, MonoGame or Godot - the brief rules out engines and frameworks. The game loop,
collision, rendering and netcode are all ours.

**New to the project? Read [docs/how-it-works.md](docs/how-it-works.md)** - it walks through every
project, how the server and clients communicate, and what happens between a keypress and your
character moving. [docs/architecture.md](docs/architecture.md) covers why the constraints shaped
the design the way they did.

## Getting started

### 1. Prerequisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download)** - check with `dotnet --version`,
  which should print `10.x`
- **Windows** for the client (it is WinForms). The server runs on any OS.
- Any editor: Visual Studio 2022, Rider, or VS Code with the C# Dev Kit.

### 2. Get the code and build it

```bash
git clone <repo-url>
cd unhallowed
dotnet build
```

The first build restores NuGet packages and takes a minute. You should see
`Build succeeded. 0 Error(s)`.

### 3. Start the server

The game is server-authoritative, so **the server must be running before any client starts.**
Open a terminal and leave this running:

```bash
dotnet run --project src/Unhallowed.Server
```

You should see:

```
Match loop started: 30 Hz simulation, 15 Hz snapshots
Now listening on: http://localhost:5080
```

Check it in a browser at <http://localhost:5080/> - it returns JSON describing the server and its
active matches.

### 4. Start the game clients

Open a **separate terminal for each player** (up to 4) and run:

```bash
dotnet run --project src/Unhallowed.Client
```

A dialog appears asking for three things:

| Field | What to enter |
| --- | --- |
| **Server** | `http://localhost:5080` on one machine, or `http://<host-ip>:5080` over LAN |
| **Match code** | Any word. **Everyone who types the same code plays together.** |
| **Name** | Your display name |

Click **Join** and the game window opens.

To skip the dialog entirely, pass the three values as arguments:

```bash
dotnet run --project src/Unhallowed.Client -- http://localhost:5080 default Ana
```

### 5. Play

| Input | Action |
| --- | --- |
| `W` `A` `S` `D` | Move |
| Arrow keys | Shoot |

You are the character with a white box drawn around it. Red circles are enemies, the purple one
is a boss. Health bars float above anything that can be hurt, and the event log runs along the
bottom-left.

### Playing across several machines

1. Run the server on one machine and find its LAN IP (`ipconfig` on Windows).
2. Allow inbound TCP port **5080** through that machine's firewall.
3. Everyone else enters `http://<that-ip>:5080` in the Server field.

The server binds to all interfaces by default - see `Kestrel` in
`src/Unhallowed.Server/appsettings.json` to change the port.

### Running everything from one terminal (quick test)

```bash
# terminal 1
dotnet run --project src/Unhallowed.Server

# terminal 2, 3, 4, 5 - one per player
dotnet run --project src/Unhallowed.Client -- http://localhost:5080 lobby1 Ana
dotnet run --project src/Unhallowed.Client -- http://localhost:5080 lobby1 Bob
```

### Run the pattern demos

Every pattern is demonstrable from a `main()` method, as the course requires. The server does
**not** need to be running for these:

```bash
dotnet run --project src/Unhallowed.PatternDemos                  # the catalogue
dotnet run --project src/Unhallowed.PatternDemos -- decorator     # one pattern
dotnet run --project src/Unhallowed.PatternDemos -- 9             # same, by number
dotnet run --project src/Unhallowed.PatternDemos -- all           # everything implemented
```

### Run the tests

```bash
dotnet test
```

### Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| Client shows "Connection failed" | The server is not running, or the URL is wrong. Start the server first and check <http://localhost:5080/> responds. |
| `Match 'x' already has 4 players` | The lobby is full. Use a different match code. |
| Players cannot see each other | They used different match codes. The code must match exactly. |
| Nothing moves, window is empty | The server is up but you have not joined - check the client terminal for an exception. |
| `Address already in use` on the server | Port 5080 is taken. Change the port in `src/Unhallowed.Server/appsettings.json`. |
| Another machine cannot connect | Firewall is blocking TCP 5080, or you used `localhost` instead of the server's LAN IP. |
| Build fails with an SDK error | Wrong .NET version. `dotnet --version` must report 10.x. |

## Layout

```
src/
  Unhallowed.Contracts/    JSON wire DTOs and protocol constants
  Unhallowed.Core/         domain, simulation, and most of the patterns
  Unhallowed.Persistence/  SQLite repositories (raw ADO.NET, no ORM)
  Unhallowed.Server/       ASP.NET Core host, SignalR hub, authoritative loop
  Unhallowed.Client/       WinForms window, GDI+ adapter, input, interpolation
  Unhallowed.PatternDemos/ console main() harness, one demo per pattern
tests/
  Unhallowed.Tests/        xUnit
docs/
  how-it-works.md          full walkthrough - start here
  architecture.md          why the technology choices were forced by the brief
  network-protocol.md      every message, with example JSON
  pattern-assignments.md   all 23 patterns, owners, status
  patterns/                one report per pattern
  uml/                     MagicDraw exports
```

`Unhallowed.Core` never references `System.Drawing`, WinForms or SignalR. The server runs the
same simulation headless, which is what forces the clean boundaries.

## Design patterns

All 23 GoF patterns are mapped onto real features so the team can deliberately drop one and still
hit 22. Eight are scaffolded with working code and a runnable demo; the rest have a planned home
and a report stub waiting.

| Status | Patterns |
| --- | --- |
| Scaffolded | Singleton, Adapter, Decorator, Facade, Command, Memento, Observer, State, Strategy, Template Method |
| Planned | Factory Method, Abstract Factory, Builder, Prototype, Bridge, Composite, Flyweight, Proxy, Chain of Responsibility, Interpreter, Iterator, Mediator, Visitor |

Full table with owners and status: [docs/pattern-assignments.md](docs/pattern-assignments.md).

Patterns are organised by feature rather than in a `Patterns/` folder - a pattern is a shape the
code takes, not a place it lives. To find one:

```bash
grep -rn "PATTERN:" src/
```

## Contributing

See [CLAUDE.md](CLAUDE.md) for the working rules. The short version:

- One branch and one PR per pattern or feature - never commit to `master`
- Comments only where the logic is genuinely hard; let names do the work
- `dotnet build` and `dotnet test` must pass before you open a PR
