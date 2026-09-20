# Architecture

## Why this shape

The course brief eliminates most of the choices before we make them:

| Rule | Consequence |
| --- | --- |
| No frameworks or engines (Unity named) | No Unity, MonoGame, Godot or Stride. We write the game loop, collision and rendering ourselves. |
| C#, desktop or web | C# was fixed by the team. |
| Primitive graphics, `System.Drawing` named | WinForms + GDI+. This is the C# counterpart of the `javax.swing` the brief names for Java. |
| Client-server via WebSockets / SignalR / REST, JSON or XML | SignalR over WebSockets with JSON. REST cannot carry a realtime game; raw WebSockets is more plumbing for no benefit. |
| Objects may be stored in a database | SQLite through raw ADO.NET. |

**Desktop, not web.** A browser client would force TypeScript for the client half, because C# in
the browser means Blazor WebAssembly plus JavaScript canvas interop - which fights the
"primitive graphics" rule rather than satisfying it. Desktop keeps everything in one language and
makes `System.Drawing` the obvious, brief-compliant answer.

## Processes

```
   +------------------------+          +------------------------+
   |  Unhallowed.Client #1  |          |  Unhallowed.Client #2  |   ... up to 4
   |  WinForms + GDI+       |          |  WinForms + GDI+       |
   |  renders snapshots     |          |  renders snapshots     |
   +-----------+------------+          +-----------+------------+
               |                                   |
               |  SignalR / WebSockets / JSON      |
               |  up:   PlayerCommandDto  (~30/s)  |
               |  down: WorldSnapshotDto  (15/s)   |
               |                                   |
               +-----------------+-----------------+
                                 |
                  +--------------v---------------+
                  |     Unhallowed.Server        |
                  |  ASP.NET Core + SignalR hub  |
                  |  authoritative World @ 30Hz  |
                  |  SQLite for definitions      |
                  +------------------------------+
```

One server process hosts any number of matches, keyed by lobby code. Each match owns one `World`.

## Projects

| Project | Target | Role |
| --- | --- | --- |
| `Unhallowed.Contracts` | `net10.0` | JSON DTOs and protocol constants. Referenced by everything; references nothing. |
| `Unhallowed.Core` | `net10.0` | Domain, simulation, and most of the design patterns. **No UI types, no networking types.** |
| `Unhallowed.Persistence` | `net10.0` | SQLite repositories, raw ADO.NET. |
| `Unhallowed.Server` | `net10.0` | ASP.NET Core host, SignalR hub, fixed-step loop. |
| `Unhallowed.Client` | `net10.0-windows` | WinForms window, GDI+ adapter, input, interpolation. |
| `Unhallowed.PatternDemos` | `net10.0` | Console `main()` harness, one demo per pattern (requirement 10). |

### The one rule that matters

**`Unhallowed.Core` never references `System.Drawing`, WinForms, or SignalR.**

The server runs the same simulation code with no UI at all, so any UI type in Core would break it
immediately. That constraint is not bureaucratic - it is what forces the Adapter at the render
boundary and the Command objects at the network boundary, and it is why the pattern demos can
exercise real game logic from a plain console app.

## Server authority

Clients are dumb on purpose:

1. The client reads the keyboard and sends **intents** (`Move`, `Shoot`) as `PlayerCommandDto`.
2. The hub validates the sender, translates the DTO into an `IGameCommand`, and drops it in that
   player's queue. The hub itself runs no game logic.
3. The fixed-step loop drains every queue once per tick, then advances the world. The world is
   only ever touched from this one thread.
4. The loop captures a snapshot and broadcasts it.
5. The client interpolates between the last two snapshots and draws.

A client can therefore lie about what it *wants* and never about what *is*. Speed is clamped
server-side, damage is calculated server-side, and an unknown command type degrades to a no-op
instead of crashing the loop.

### Rates

| Thing | Rate | Why |
| --- | --- | --- |
| Simulation | 30 Hz | Enough for an Isaac-like; a fixed step keeps physics deterministic. |
| Snapshots | 15 Hz | Halves bandwidth; interpolation hides the gap. |
| Client input | ~30 Hz, on change only | The server keeps applying the last intent, so idle players send nothing. |
| Client render | ~60 FPS | Decoupled from both of the above. |

All four are constants in `Unhallowed.Contracts/Protocol.cs`.

### Interpolation

The client renders **100 ms in the past** and interpolates between the two snapshots bracketing
that time. Without it, entities visibly step 15 times a second. `SnapshotBuffer` holds the last
dozen snapshots and does the lerp.

This is also why client-side prediction is *not* implemented yet: it is worth adding for the
local player only (so your own movement feels instant), and it pairs naturally with the Memento
rollback that already exists server-side. Good optional scope if the team has time.

## Where the patterns live

Patterns are organised **by feature, not in a `Patterns/` folder**. A pattern is a shape the code
takes, not a place it lives - and a `Patterns/Strategy/` directory would be the strongest possible
evidence that the patterns were bolted on rather than designed in.

To find one fast during a defence:

```
grep -rn "PATTERN:" src/
```

Every pattern's key type carries a `// PATTERN: <Name> -> docs/patterns/NN-x.md` marker.
`docs/pattern-assignments.md` has the full map.

## Deliberate gaps

Things a production game would have that this one does not, and why that is fine here:

- **No client-side prediction.** Movement has one round-trip of latency. Fine on LAN or
  localhost, which is where this will be demonstrated.
- **No reconciliation or rollback on the client.** The server-side `SnapshotHistory` exists and
  is demonstrable, but nothing consumes it for lag compensation yet.
- **No room-to-room transitions.** One room per match so far. `World.EnterRoom` already handles
  the swap; the dungeon graph that decides *which* room is the Builder / Iterator work.
- **No persistence of runs.** The database holds definitions, not save games.
- **Collision is O(n x m) brute force.** With a handful of entities per room this is irrelevant,
  and a spatial grid would be the natural home for the Flyweight work if someone wants it.
