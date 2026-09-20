# How Unhallowed works

A complete walkthrough of the system: what the pieces are, why they are split that way, and what
happens between pressing a key and seeing your character move.

Read this before your first defence. If you can explain [the summary at the end](#the-one-paragraph-version)
in your own words, you understand the project.

**Contents**

1. [What this actually is](#1-what-this-actually-is)
2. [The projects, one by one](#2-the-projects-one-by-one)
3. [The one design decision everything follows from](#3-the-one-design-decision-everything-follows-from)
4. [The server's heartbeat](#4-the-servers-heartbeat)
5. [How the two sides talk](#5-how-the-two-sides-talk)
6. [One keypress, end to end](#6-one-keypress-end-to-end)
7. [Why the client renders the past](#7-why-the-client-deliberately-renders-the-past)
8. [Where the patterns live](#8-where-the-patterns-actually-live)
9. [What deliberately is not there](#9-what-deliberately-is-not-there)
10. [The one-paragraph version](#the-one-paragraph-version)

---

## 1. What this actually is

Three kinds of program, written by us, with no game engine underneath.

| Program | What it is | How many run |
| --- | --- | --- |
| **Server** | A console app that simulates the game | 1 |
| **Client** | A Windows window that draws pictures and reads your keyboard | up to 4 |
| **PatternDemos** | A separate console app used during defences | on demand |

The critical thing to understand: **the client is not the game.** The client is a dumb terminal.
It knows how to draw circles and how to say "the user is holding W". It does not know what a
player is, how much damage a tear does, or whether an enemy died. All of that lives on the server.

If you closed every client, the match would keep running on the server with nobody watching.

---

## 2. The projects, one by one

Seven projects. Here is what each one is for and, more importantly, **why it exists as a separate
project** rather than being folded into another.

```
Contracts  <-- referenced by everything, references nothing
    ^
    |
  Core     <-- the game itself. NO UI types. NO networking types.
    ^
    +--------------+---------------+----------------+
    |              |               |                |
Persistence     Server          Client        PatternDemos
 (SQLite)     (+ASP.NET)      (+WinForms)      (console)
```

Arrows point from a project to what it depends on. Notice that nothing points *into* Core from the
side — Core does not know that the server, the client or the database exist.

### `Unhallowed.Contracts` — the shared vocabulary

**What it is:** the data shapes and constant strings that the client and server must agree on.
Nothing else. No logic, no behaviour, just `record` types and `const string`s.

**What is in it:**

| File | Contents |
| --- | --- |
| `Protocol.cs` | Hub path, tick rate, snapshot rate, max players, and the names of every callable method |
| `Messages/ClientMessages.cs` | `JoinMatchRequest`, `PlayerCommandDto` — things a client sends |
| `Messages/ServerMessages.cs` | `MatchJoinedResponse`, `WorldSnapshotDto`, `EntitySnapshotDto`, `GameEventDto`, `PlayerPresenceDto` — things a server sends |

**Why it is separate.** The client and server are two different programs that must speak the same
language. That language needs to live somewhere both can see.

- If these types lived in **Core**, the client would have to reference the entire simulation —
  every enemy, every strategy, the whole world model — just to deserialise a snapshot of circles.
- If they lived in **Server**, the client could not see them at all, and you would end up
  hand-writing the same DTO twice in two places, which drifts the moment someone edits one.

Putting them in their own tiny project that depends on *nothing* makes the contract explicit.
It also makes breakage loud: if you change a field in `WorldSnapshotDto`, every project that
speaks the protocol recompiles, and a mismatch is a build error rather than a silent runtime
failure at 3am before a demo.

**Rule of thumb:** if a type crosses the network, it belongs here. If it has a method that *does*
something, it does not.

### `Unhallowed.Core` — the game itself

**What it is:** the domain model and the simulation. This is where most of your coursework
happens, and where most of the design patterns live.

**What is in it:**

| Folder | Contents | Pattern |
| --- | --- | --- |
| `Common/` | `Vec2` maths, `ServiceRegistry` | Singleton |
| `Entities/` | `Entity`, `Player`, `Enemy`, `Projectile`, `IWorldView` | |
| `Entities/Stats/` | `IPlayerStats` and the item wrappers | Decorator |
| `AI/` | `IMovementStrategy` and its four implementations | Strategy |
| `States/` | `IPlayerState`: Idle, Moving, Hurt, Dead | State |
| `Commands/` | `IGameCommand`, `CommandQueue`, `CommandTranslator` | Command |
| `Rooms/` | `RoomBase` and the room types | Template Method |
| `Events/` | `EventBus`, `IGameEventObserver` | Observer |
| `Simulation/` | `World`, `WorldSnapshot`, `SnapshotHistory` | Memento |
| `Rendering/` | `IRenderSurface` — the interface the client adapts to | Adapter (target) |

**The rule that defines this project:**

> **Core never references `System.Drawing`, WinForms, or SignalR.**

This sounds like architectural purity for its own sake until you see what it buys:

1. **The server can run the simulation headless.** It has no window. If `Player` imported
   `System.Drawing.Color`, the server would not start.
2. **The pattern demos exercise real game code from a console app.** When the Strategy demo prints
   enemy positions, that is the actual `ChaserStrategy` the game uses, not a toy copy.
3. **It forces the patterns to exist for real reasons.** Core needs to draw but cannot touch GDI+,
   so it declares `IRenderSurface` and the client supplies an Adapter. That is not a pattern
   bolted on for marks — it is the only way to satisfy the constraint. Which is exactly the answer
   you want when an examiner asks "why Adapter here?"

### `Unhallowed.Persistence` — the database layer

**What it is:** SQLite storage for enemy and room *definitions*, using raw ADO.NET.

**What is in it:**

| File | Contents |
| --- | --- |
| `GameDatabase.cs` | Connection management, `CREATE TABLE` schema, one-time seed data |
| `EnemyDefinitionRepository.cs` | `EnemyDefinition` / `RoomDefinition` records, an `IEnemyDefinitionRepository` interface, a SQLite implementation and an in-memory one for tests |

Two tables: `EnemyDefinition` (archetype, health, contact damage, radius, movement kind, speed) and
`RoomDefinition` (id, type, size, enemy budget). The seed rows are the gaper, fly, charger, turret
and monstro.

**Why raw ADO.NET and not Entity Framework.** Rule 3 bans frameworks. Beyond that, an ORM would
*hide* the repository structure — and the repository structure is the point. Hand-written SQL keeps
the data access visible and defensible.

**Important, be honest about this:** Persistence is currently **referenced but never called**.
The server has a project reference to it, but nothing constructs a `GameDatabase`. Enemy stats are
presently hard-coded in `RoomTypes.Populate()`.

That is deliberate groundwork, not an oversight. It is the natural home for two planned patterns:

- **Prototype (05)** — load definitions once at startup, then clone per spawn instead of hitting
  the database on every enemy.
- **Factory Method (02)** — an `EnemyFactory` that reads a definition and builds a configured
  `Enemy`, replacing the direct `new Enemy(...)` calls.

Whoever takes either pattern wires this in. Requirement 7 says objects *may* be stored in a
database — it is credit, not an obligation, so this is worth doing but is not blocking.

### `Unhallowed.PatternDemos` — the defence tool

**What it is:** a console application whose entire job is to satisfy requirement 10 — *"pattern
operation must be demonstrated through a working `main()` method."*

**What is in it:**

| File | Contents |
| --- | --- |
| `Program.cs` | The `main()` entry point and argument parsing |
| `IPatternDemo.cs` | The interface every demo implements, plus console formatting helpers |
| `PatternCatalog.cs` | **All 23 GoF patterns**, each with its number, category, planned game use, owner and demo (or `null` if unimplemented) |
| `Demos/` | The demo implementations |

**Why it is separate from the tests.** Tests prove code is correct; they print nothing and a
lecturer cannot read them over your shoulder. These demos are *narrated* — they print what they are
doing and why, so you can run one during a defence and point at the output.

```bash
dotnet run --project src/Unhallowed.PatternDemos                # the catalogue, with status
dotnet run --project src/Unhallowed.PatternDemos -- decorator   # one pattern
dotnet run --project src/Unhallowed.PatternDemos -- 9           # the same, by number
dotnet run --project src/Unhallowed.PatternDemos -- all         # every implemented demo
```

`PatternCatalog.cs` doubles as the project's to-do list. Running it with no arguments prints all 23
patterns marked `[demo]` or `[todo]` with their owner, so the team can see progress at a glance.

**When you implement a new pattern, register its demo here** or it does not count for requirement 10.

### `Unhallowed.Server` — the authority

**What it is:** an ASP.NET Core application hosting the SignalR hub and the authoritative game loop.

| File | Role |
| --- | --- |
| `Program.cs` | Wires up SignalR with the JSON protocol, registers services, maps the hub and a status endpoint |
| `Hubs/MatchHub.cs` | The only network entry point: join, leave, send command |
| `Matches/Match.cs` | One lobby — a `World`, an `EventBus`, a snapshot history and the connection-to-player map |
| `Matches/MatchManager.cs` | All live matches, keyed by lobby code |
| `Matches/MatchLoop.cs` | The fixed-timestep loop, running as a `BackgroundService` |
| `Matches/SignalREventBroadcaster.cs` | An Observer that relays game events to clients |

ASP.NET Core is allowed here *only* because the brief names SignalR as a permitted network
technology. It is not being used as a game framework, and no game logic lives in it.

### `Unhallowed.Client` — the window

**What it is:** a WinForms application that draws snapshots with GDI+ and sends keystrokes.

| File | Role |
| --- | --- |
| `Program.cs` | `main()`, argument parsing, shows the join dialog |
| `ConnectForm.cs` | The small server / match code / name dialog |
| `GameForm.cs` | The game window: input timer at ~30 Hz, render timer at ~60 FPS |
| `Net/GameClient.cs` | Facade over SignalR, JSON, reconnect and sequence numbers |
| `Net/SnapshotBuffer.cs` | Holds recent snapshots and interpolates between them |
| `Rendering/GdiRenderSurface.cs` | The Adapter over `System.Drawing.Graphics` |
| `Rendering/WorldRenderer.cs` | Draws the world using *only* `IRenderSurface` |
| `Rendering/Palette.cs` | Maps abstract `PaletteColor` values to real colours |

This is the only project that targets `net10.0-windows`, because WinForms is Windows-only.

Note that `WorldRenderer` contains no `System.Drawing` type at all — it draws through the
interface. That is what lets the Adapter demo swap in an ASCII backend and get the same picture.

### `tests/Unhallowed.Tests` — the safety net

xUnit. 26 tests covering the patterns that carry real logic: decorator stacking, command
deduplication, state transitions, memento round-trips, room lifecycle, strategy behaviour and
singleton identity.

```bash
dotnet test
```

These must pass before you open a PR.

---

## 3. The one design decision everything follows from

**The server is authoritative.** This is the single most important concept in the project, and the
thing an examiner is most likely to probe.

Consider the alternative. Suppose each client simulated its own game and told the others "I moved
to x=400, and I killed that enemy." Now:

- Two clients disagree about where an enemy is. Who is right? Nobody knows.
- A client can simply claim `x = 999999` and teleport.
- Two players shoot the same enemy in the same instant. Does it die once or twice?

These are not hypotheticals, they are the default outcome. The fix is to declare **one process the
sole owner of truth**.

> Clients send **intent**. The server decides **reality**. Clients render what they are told.

A client can lie about what it *wants* ("I want to move right"). It cannot lie about what *is*,
because it is never asked. Look at `MatchHub` — there is no method a client can call that sets a
position, deals damage, or kills anything. The entire client-facing API is three methods: join,
leave, and "here is what I want".

---

## 4. The server's heartbeat

The server runs a **fixed-timestep loop** in `MatchLoop`, which is a `BackgroundService` — ASP.NET
Core starts it automatically and it runs for the lifetime of the process.

```
30 times per second, for every active match:
    1. Drain each player's command queue and apply the commands
    2. Update every entity by exactly 1/30th of a second
    3. Resolve collisions
    4. Add newly spawned entities
    5. Remove dead entities and raise their death events
    6. Ask the room whether it has been cleared
    7. Capture a snapshot
    8. Every 2nd tick, broadcast that snapshot to all clients
```

### Why *fixed* timestep

Every update advances the world by exactly `1f / 30f` seconds — never by "however long the last
frame took".

If you used real elapsed time, a laggy server would produce a different simulation than a fast one.
A player moving at 140 px/s through one slow 200 ms frame would jump 28 pixels, straight through a
wall. With a fixed step, the physics behave identically regardless of machine speed. It also makes
the Memento rollback meaningful: replaying ticks 10 to 30 gives the same answer every time.

### Why 30 Hz simulation but 15 Hz snapshots

Simulating at 30 Hz is cheap — it is arithmetic in memory. *Sending* at 30 Hz costs bandwidth for
every connected player. Halving the send rate halves the network cost, and the client hides the gap
by interpolating (section 7). They are separate numbers on purpose, and both are constants in
`Protocol.cs`.

### Catching up when it falls behind

```csharp
var delayMs = nextTickMs - stopwatch.Elapsed.TotalMilliseconds;
if (delayMs > 0)
{
    await Task.Delay(TimeSpan.FromMilliseconds(delayMs), stoppingToken);
}
else
{
    nextTickMs = stopwatch.Elapsed.TotalMilliseconds;
}
```

If a tick overran, the loop **forgives the lost time** rather than running extra ticks to catch up.
Trying to catch up is how you get a death spiral: you are behind, so you run more ticks, which puts
you further behind. Better to drop the lost time.

---

## 5. How the two sides talk

### What SignalR is

SignalR is a library on top of **WebSockets**. A WebSocket is a TCP connection that stays open —
unlike plain HTTP, where you ask a question, get an answer and hang up. That persistent connection
is what lets the server *push* without being asked, which is exactly what a game needs.

What SignalR adds:

- **Remote procedure calls.** Instead of framing bytes by hand you write
  `conn.InvokeAsync<MatchJoinedResponse>("JoinMatch", request)` and it reads like a method call.
- **Automatic JSON serialisation** both ways, which satisfies the brief's JSON requirement for free.
- **Groups.** `Clients.Group("lobby1")` reaches everyone in a lobby without tracking the list yourself.
- **Reconnection** and transport fallback if WebSockets are blocked.

The hub lives at `http://localhost:5080/hub/match`.

### The vocabulary

Everything either side can say is declared in `Protocol.cs`, so both halves compile against the
same names and a typo is a build error rather than a silent runtime failure:

```csharp
public static class ClientToServer   // clients call these ON the server
{
    public const string JoinMatch   = nameof(JoinMatch);
    public const string LeaveMatch  = nameof(LeaveMatch);
    public const string SendCommand = nameof(SendCommand);
}

public static class ServerToClient   // the server calls these ON clients
{
    public const string Snapshot     = nameof(Snapshot);
    public const string GameEvent    = nameof(GameEvent);
    public const string PlayerJoined = nameof(PlayerJoined);
    // ...
}
```

### Joining a match

```
client                                       server
  |                                            |
  |-- JoinMatch({ "default", "Ana" }) -------->|  MatchManager.GetOrCreate("default")
  |                                            |  -> creates a World, populates a room
  |                                            |  -> World.AddPlayer("Ana", seat 0)
  |                                            |  -> maps connectionId -> playerId
  |<------- { PlayerId: 5, Seat: 0 } ----------|
  |                                            |
  |<------- PlayerJoined (to everyone else) ---|
  |                                            |
  |<------- Snapshot, 15 times a second -------|  until disconnect
```

**Matches are created on demand by lobby code.** `MatchManager` is a
`ConcurrentDictionary<string, Match>`. The first person to type `lobby1` creates it; everyone after
joins it. When the last player leaves it is removed. That is how one server process hosts any
number of independent games.

`PlayerId` answers "which circle on screen is me?" — the client watches for that id in every
snapshot. `Seat` (0-3) picks your colour.

---

## 6. One keypress, end to end

This is the part worth memorising. Trace pressing **D** to walk right.

### 1. The client notices

`GameForm` keeps a `HashSet<Keys>` of what is held. A timer fires every 33 ms (~30 Hz) and reads it:

```csharp
var move = ReadAxis(Keys.A, Keys.D, Keys.W, Keys.S);   // -> (1, 0)

if (move != _lastMove)
{
    _lastMove = move;
    await _client.SendMoveAsync(move.X, move.Y);
}
```

**Commands are sticky.** The server keeps applying your last `Move` until a new one arrives, so
holding D sends *one* message, not 30 per second. An idle player sends nothing at all. Releasing D
sends `(0, 0)`.

### 2. It becomes JSON

`GameClient` attaches a sequence number and ships it:

```json
{ "Sequence": 42, "Type": "Move", "X": 1.0, "Y": 0.0 }
```

The sequence comes from `Interlocked.Increment(ref _sequence)` and only ever goes up.

### 3. The hub receives it, and does nothing interesting

```csharp
public Task SendCommand(PlayerCommandDto dto)
{
    var match = matches.FindByConnection(Context.ConnectionId);
    var playerId = match?.PlayerIdFor(Context.ConnectionId);

    if (match is null || playerId is null)
    {
        return Task.CompletedTask;
    }

    match.World.EnqueueCommand(playerId.Value, CommandTranslator.FromDto(dto));
    return Task.CompletedTask;
}
```

Two things matter here.

**Identity comes from the connection, not the message.** `Context.ConnectionId` is SignalR's and a
client cannot forge it, so you physically cannot send a command "as" another player. There is no
field for it.

**The hub runs no game logic.** It translates and queues. Why? Because SignalR delivers calls on
**arbitrary thread-pool threads**, many at once. If the hub touched the world directly, four players
moving simultaneously would corrupt the entity list. Instead it drops into a lock-protected queue
and the simulation thread — a single thread — drains it.

### 4. The DTO becomes an object again

```csharp
return dto.Type switch
{
    "Move"  => new MoveCommand(dto.Sequence, direction),
    "Shoot" => new ShootCommand(dto.Sequence, direction),
    _       => new NoOpCommand(dto.Sequence),
};
```

That `_ =>` case matters: a malformed or newer client sending `"Teleport"` gets a no-op, not a
crash. **Never let client input reach a `throw` inside the game loop.**

### 5. The queue filters it

```csharp
if (command.Sequence <= LastAcceptedSequence)
{
    return;
}

LastAcceptedSequence = command.Sequence;
_pending.Enqueue(command);
```

Packets can arrive twice or out of order once retries and reconnects are in play. Sequence numbers
make the server immune to replayed messages.

### 6. The next tick applies it

Up to 33 ms later, `World.Step` drains every queue:

```csharp
foreach (var command in inbox.Drain())
{
    command.Execute(player, this);
}
```

And `MoveCommand.Execute` does the validation:

```csharp
var clamped = direction.Length > 1f ? direction.Normalized() : direction;
player.MoveIntent = clamped;
```

**This is the anti-cheat.** A client sending `(100, 0)` hoping to move 100 times faster gets
normalised to `(1, 0)`. Speed never comes from the client — it comes from `player.Stats.MoveSpeed`
on the server.

Note that it sets `MoveIntent`, not `Position`. Intent, not truth.

### 7. The state machine turns intent into movement

`Player.Update` delegates to its current state object:

```csharp
// MovingState.Update
player.Velocity = player.MoveIntent.Normalized() * player.Stats.MoveSpeed;
```

Then `Position += Velocity * deltaSeconds`, then clamp to the room bounds.

### 8. Snapshot and broadcast

Every 2nd tick the world is photographed and sent to every connection in the match:

```json
{
  "Tick": 1530,
  "RoomId": "default-r1",
  "AckSequence": 42,
  "Entities": [
    { "Id": 5, "Kind": "player", "X": 484.6, "Y": 270.0,
      "Radius": 12.0, "Health": 6, "MaxHealth": 6, "Visual": "player0" },
    { "Id": 7, "Kind": "enemy", "X": 120.5, "Y": 88.2,
      "Radius": 14.0, "Health": 5, "MaxHealth": 8, "Visual": "gaper" }
  ]
}
```

Snapshots are **full, not delta-encoded** — every entity, every time. With four players and a room
of enemies that is a few kilobytes per second, and it makes the protocol trivially debuggable: read
one snapshot and you know the entire world state. Delta encoding is the optimisation if entity
counts ever grow.

### 9. The client draws it

And the loop closes. Total latency: up to 33 ms waiting for the input timer, plus network, plus up
to 33 ms waiting for the next tick, plus up to 66 ms for the next snapshot, plus the 100 ms
interpolation delay.

---

## 7. Why the client deliberately renders the past

Snapshots arrive **15 times a second**, but the window redraws **60 times a second** — four frames
drawn per snapshot received. Naively, everything would freeze for three frames then jump. Visible,
ugly stuttering.

`SnapshotBuffer` solves this by rendering **100 milliseconds in the past**:

```
real time:     ----------------------------------|now
snapshots:     A         B         C         D
render time:                            ^
                                   (now - 100ms)
                              between C and D -> interpolate
```

Because it is deliberately behind, the two snapshots bracketing the render time have *already
arrived*, so it can smoothly interpolate between them:

```csharp
result.Add(current with
{
    X = previous.X + ((current.X - previous.X) * t),
    Y = previous.Y + ((current.Y - previous.Y) * t),
});
```

The trade: 100 ms of added visual latency in exchange for perfectly smooth motion. Every networked
game makes this trade.

This is also why **your own movement feels slightly delayed.** The fix is client-side prediction —
simulate your own player locally and correct when the server disagrees — which is deliberately not
implemented. It is good optional scope, and it pairs with the `AckSequence` field that already
exists in the protocol but is currently unused.

---

## 8. Where the patterns actually live

There is no `Patterns/` folder, on purpose. A pattern is a *shape the code takes*, not a place — and
a `Patterns/Strategy/` directory would be the strongest possible evidence that the patterns were
bolted on afterwards. Find them by grepping instead:

```bash
grep -rn "PATTERN:" src/
```

The ten that are built, and what each is genuinely load-bearing for:

| Pattern | Where | The problem it solves |
| --- | --- | --- |
| **Command** | `Core/Commands/` | Player intents must survive serialisation, queuing, deduplication and a thread hop. An object can do that; a method call cannot. |
| **Memento** | `Core/Simulation/WorldSnapshot.cs` | `World` captures itself; `SnapshotHistory` stores mementos without reading them. The same object is the wire format *and* the rollback buffer. |
| **Observer** | `Core/Events/` | The simulation raises "enemy died" without knowing SignalR exists. `SignalREventBroadcaster` just subscribes. |
| **State** | `Core/States/` | Idle / Moving / Hurt / Dead as objects. Invulnerability is `HurtState.IsInvulnerable`, not a bool and a timer scattered through `Player`. |
| **Strategy** | `Core/AI/` | One `Enemy` class, four movement behaviours. `Enemy.Update` contains no branch on enemy type. |
| **Decorator** | `Core/Entities/Stats/` | Isaac's whole identity. Items wrap `IPlayerStats`; ten items is a ten-link chain and `Player` never changes. |
| **Template Method** | `Core/Rooms/RoomBase.cs` | `Enter()` is non-virtual — subclasses change the *steps*, never the *order*. |
| **Adapter** | `Client/Rendering/GdiRenderSurface.cs` | Core cannot see GDI+, so it draws through `IRenderSurface`. |
| **Facade** | `Client/Net/GameClient.cs` | Hides SignalR setup, JSON, reconnect, sequence counters and interpolation behind three methods. |
| **Singleton** | `Core/Common/ServiceRegistry.cs` | Thread-safe lazy initialisation, private constructor, sealed. |

One thing to know about `RoomTypes.Populate()`: it calls `new Enemy(...)` directly. **That is
intentional.** Requirement 9 wants a before-and-after UML diagram, and that code is the genuine
"before" for the Factory Method report. Whoever takes pattern 02 gets a real refactor to diagram
rather than an invented one.

The remaining thirteen patterns and their planned homes are in
[pattern-assignments.md](pattern-assignments.md).

---

## 9. What deliberately is not there

Worth knowing so you are not caught out by a question.

- **No client-side prediction.** Your own movement has one round trip of latency.
- **Nothing consumes the rollback.** `SnapshotHistory` works and is demonstrable, but no lag
  compensation uses it yet.
- **One room per match.** `World.EnterRoom` handles the swap correctly; the dungeon *graph* is the
  Builder and Iterator work.
- **The database is not wired in.** See the Persistence section above.
- **Collision is brute-force O(n x m).** Irrelevant at these entity counts, and it is the natural
  home for Flyweight if someone wants it.
- **Reconnecting creates a new player**, not a resumed one.

---

## The one-paragraph version

> The server runs an authoritative fixed-timestep simulation at 30 Hz. Clients connect over
> SignalR/WebSockets and send serialised Command objects describing intent, never state. The hub
> identifies the sender by connection id and queues those commands without touching the world,
> because SignalR delivers on arbitrary threads while the simulation is single-threaded. Each tick
> the world drains the queues, updates entities, resolves collisions and captures a Memento. Every
> second tick that Memento is serialised to JSON and broadcast. Clients buffer snapshots, render
> 100 ms in the past so they can interpolate smoothly, and draw through an Adapter over
> System.Drawing so the domain layer stays free of UI types — which is what lets the server run the
> same simulation headless.

## See also

- [architecture.md](architecture.md) — why the technology choices were forced by the brief
- [network-protocol.md](network-protocol.md) — every message with example JSON
- [pattern-assignments.md](pattern-assignments.md) — all 23 patterns, owners and status
- [../CLAUDE.md](../CLAUDE.md) — branching, commit and comment rules
