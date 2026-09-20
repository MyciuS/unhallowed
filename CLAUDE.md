# Unhallowed

2D multiplayer roguelike (Binding of Isaac style), C#, up to 4 players. University software
engineering project: the codebase must demonstrate 22 GoF design patterns.

## Workflow rules

### Branching

**Every new piece of functionality goes on its own branch.** Never commit work directly to
`master`.

```
git checkout -b <type>/<short-description>
```

Branch naming:

| Type | Use for | Example |
| --- | --- | --- |
| `pattern/` | implementing a design pattern | `pattern/09-decorator` |
| `feat/` | gameplay or system features | `feat/room-transitions` |
| `fix/` | bug fixes | `fix/projectile-lifetime` |
| `docs/` | reports, diagrams, README | `docs/strategy-report` |

One pattern per branch, one pattern per PR. The PR template has a "Design Patterns" section -
fill it in. This keeps the cumulative defence (requirement 15) straightforward: each pattern has
one reviewable, defendable PR.

### Commits

- No Claude/AI attribution lines. No `Co-Authored-By: Claude`, no "Generated with Claude Code".
  Commits are authored by the student who owns the work.
- Present tense, imperative: `add charger movement strategy`, not `added` or `adds`.
- Keep commits scoped to one logical change.

### Comments

**Write no comments unless they are genuinely necessary.** Code should explain itself through
naming and structure.

Do not write:

```csharp
// Increment the counter
counter++;

// Loop through enemies
foreach (var enemy in enemies)
```

Do write a comment when the logic is genuinely hard and the *reason* is not visible in the code:

```csharp
// Copy first: an observer is allowed to detach itself while being notified.
snapshot = [.. _observers];
```

Rules of thumb:

- Explain **why**, never **what**. The what is already in the code.
- Non-obvious algorithms, protocol quirks, race conditions, deliberate trade-offs, and
  workarounds for external behaviour are worth a comment.
- If a comment is needed to explain what a block does, extract it into a well-named method
  instead.
- One exception, specific to this project: a `// PATTERN: <Name> -> docs/patterns/NN-x.md`
  marker at the top of a pattern's key type. It exists so any team member can `grep` their way
  to the code during a defence. Keep those; they earn their place.
- XML doc comments (`///`) on public types and members are fine and encouraged - they are API
  documentation, not clutter.

## Hard constraints (from the course brief)

These are not preferences. Breaking any of them fails the project.

1. **No game engines or frameworks.** No Unity, MonoGame, Godot, Stride. ASP.NET Core is
   permitted solely because the brief names SignalR as an allowed network technology.
2. **C#** throughout.
3. **Desktop application.** WinForms client.
4. **Client-server networking** over SignalR (WebSockets), **JSON** payloads. Never peer-to-peer.
   The server is authoritative; clients send intents and render snapshots, nothing else.
5. **Primitive graphics only** - `System.Drawing` / GDI+. No sprite engine, no shaders, no
   third-party rendering library.
6. **Database is optional but credited** - SQLite via raw ADO.NET (`Microsoft.Data.Sqlite`).
   No ORM: an ORM would hide the very structure the repository classes are meant to show.
7. **Every pattern must run from `main()`** - register it in
   `src/Unhallowed.PatternDemos/PatternCatalog.cs` so it is demonstrable on demand.

## Project layout

```
src/
  Unhallowed.Contracts/    JSON wire DTOs shared by client and server
  Unhallowed.Core/         domain + simulation + the design patterns   <- most work happens here
  Unhallowed.Persistence/  SQLite repositories (raw ADO.NET)
  Unhallowed.Server/       ASP.NET Core host, SignalR hub, authoritative game loop
  Unhallowed.Client/       WinForms + GDI+ client
  Unhallowed.PatternDemos/ console main() harness, one runnable demo per pattern
tests/
  Unhallowed.Tests/        xUnit
docs/
  patterns/                one report per pattern (requirement 9)
  uml/                     MagicDraw exports (requirement 14)
```

`Unhallowed.Core` must never reference `System.Drawing` or any UI type. The client adapts GDI+ to
`IRenderSurface`; that boundary is what lets the server run the same simulation headless.

## Conventions

- File-scoped namespaces, `var` for obvious types, nullable reference types on.
- One public type per file, named after the file.
- Patterns are organised by **feature**, not in a `Patterns/` folder. A pattern is structure, not
  a location. `docs/pattern-assignments.md` maps each pattern to its files.
- New enemy behaviour = a new `IMovementStrategy`, not a branch in `Enemy.Update`.
- New item = a new `StatDecorator`, not a field on `Player`.
- New room type = a new `RoomBase` subclass, not a flag.

## Commands

```
dotnet build                                                  build everything
dotnet test                                                   run the test suite
dotnet run --project src/Unhallowed.Server                    start the server (port 5080)
dotnet run --project src/Unhallowed.Client                    start a client (repeat up to 4x)
dotnet run --project src/Unhallowed.PatternDemos              list the pattern catalogue
dotnet run --project src/Unhallowed.PatternDemos -- <key>     run one pattern demo
```

Before opening a PR: `dotnet build` and `dotnet test` must both pass.
