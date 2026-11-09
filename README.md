# Isaac Co-op: design patterns edition

A small *Binding of Isaac*-style roguelike in C# with [Raylib-cs](https://github.com/ChrisDill/Raylib-cs),
with online co-op for up to 4 players over [SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction) (WebSockets).

## Running (NixOS)

```sh
nix-shell          # provides dotnet 10 and the X11/GL libraries raylib needs
dotnet run         # main menu
dotnet run -- --solo
dotnet run -- --host            # host on TCP port 7777
dotnet run -- --join 192.168.1.5   # or ip:port
```

On other distros, `dotnet run` works as long as the system has X11/OpenGL libraries.

## Running (Windows)

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (x64).
2. From the project folder, in PowerShell or cmd:

   ```powershell
   dotnet run                     # main menu
   dotnet run -- --solo
   dotnet run -- --host
   dotnet run -- --join 192.168.1.5
   ```

Nothing else needs installing because Raylib-cs ships `raylib.dll`. The first time you host, Windows Defender
Firewall asks whether to allow `Isaac.exe`. Allow it on **private networks** so other players can connect
to TCP port 7777.

To give someone a build they can run without the SDK, publish a self-contained exe:

```sh
dotnet publish -c Release -r win-x64 --self-contained true -o publish/win-x64
```

This command also works from Linux. Zip the `publish/win-x64` folder and send it.

**Pattern demos:** `dotnet run -- --demo prototype` (or `bridge`, `facade`) runs a console demonstration instead of the game.

## Dedicated server (headless Linux)

`--server` runs the host without a window, so it works on a server with no display. It runs in Docker. All 4 slots go
to remote players, and the game restarts by itself 5 seconds after a game over or once everyone has left.

```sh
docker compose up -d --build   # builds the image and listens on TCP 7777
docker compose logs -f         # joins, leaves, restarts
```

**Controls:** WASD to move, arrow keys to shoot, Esc to go back to the menu, and R to restart after a game over (host only).

**Developer console:** F6 opens it, F6 or Esc closes it. The Cheats tab (items, heal, kill all enemies, boss room, next floor, reveal map, god mode, one-shot kills) only works in singleplayer or for the host of a co-op game, since the host runs the only real copy of the game. Items, heal, reveal map and the two toggles can be undone and redone with the Undo/Redo buttons or Ctrl+Z / Ctrl+Y. The Patterns tab lists the twelve required patterns, shows which ones are in the current build, and runs their `--demo`s in-game.

## Gameplay

- Floors are random and use a minimap. Each floor has normal rooms, a treasure room (yellow) and a boss room (red).
- Every 3rd floor is a **Boss Rush**: a corridor of boss rooms (3 on floor 3, up to 5 later) with a treasure room
  next to the start. Each boss before the last one drops a heart.
- Doors stay locked until every enemy in the room is dead. Cleared rooms can drop hearts.
- Enemies: Fly, Gaper (walks around rocks), Spitter (keeps a clear shot at you, ducks behind rocks when you approach), Charger (dashes until it hits a wall),
  Spider (skitters at or around you) and the boss Monstro, which bounces around spitting sprays of tears, then enrages and hops around firing rings.
- Items: Damage Up, Tears Up, Speed Up, Range Up, Heart Container, Glass Cannon.
- After you beat the last boss, take the trapdoor to the next floor. Enemies get stronger and players who died come back.

## Multiplayer architecture

The host is authoritative and the clients only render ("dumb clients").

```
 client                                   host
 PlayerInput.ReadKeyboard() --(SignalR)-->  GameHub -> PlayerInput.ApplyTo(player)
                                             World.Update() at a fixed 60 Hz
 Renderer.Draw(snapshot) <--(SignalR)--     World.BuildSnapshot() -> JSON
```

- Clients never run game logic. They send their input and draw the newest `Snapshot`.
- The host draws exactly the same `Snapshot` object, so host and clients share one render path.
- The host runs an embedded ASP.NET Core server with a SignalR hub at `/game`. Hub calls arrive on
  thread pool threads, so `GameHub` only queues them and `HostSession` applies them on the game thread.
- Input and snapshots are sent as JSON objects with the SignalR JSON hub protocol. Host and client share
  the serializer settings in `NetJson` (public fields included, enums as numbers, default values left out).
  Clients only send their input when it changes.
- SignalR runs over TCP and never drops data, and a send finishes once the message is buffered, not when it
  arrives. So clients ack every snapshot, and the host stops sending to a client that has
  `Config.MaxSnapshotsInFlight` unacked ones. On a slow link the client gets fewer but current snapshots
  instead of a growing backlog of old ones.

## Design patterns

Eleven patterns are implemented so far. The rest of the code is intentionally plain, so other
team members can add their own patterns.

| Pattern | Where | What it does |
|---|---|---|
| **Singleton** | `Core/GameLogger.cs` | One thread-safe log for the whole program, reached through `GameLogger.Instance`. A private constructor and `Lazy<T>` make sure only one instance is created, and a lock keeps the game thread and SignalR threads from mixing up lines. Writes to the console and `game.log`. |
| **Observer** | `Core/EventBus.cs`, `Game/Events.cs`, `Game/Listeners.cs` | `ScoreKeeper`, `Announcer`, `LootDropper` and `ActionLogger` react to world events without being coupled to the code that raises them |
| **Builder** | `Game/Dungeon/` | `FloorDirector` (Director) runs the same steps (`Reset`, `BuildLayout`, `AssignRoomKinds`, `AddObstacles`, `AddEnemies`, `AddRewards`, `GetResult`) on an `IFloorBuilder` (Builder). `StandardFloorBuilder` and `BossRushFloorBuilder` (Concrete Builders) produce a `Floor` (Product). `DungeonFacade.CreateFloor` (Client) picks the builder for each floor, and `World` goes through the facade. |
| **Abstract Factory** | `Game/Entities/EnemyFactory.cs`, `Game/Entities/Enemy.cs` | `IEnemyFactory` defines creation methods for a family of related enemies (`CreateSwarmEnemy`, `CreateChaserEnemy`, `CreateShooterEnemy`, `CreateRusherEnemy`, `CreateSkitterEnemy`, `CreateBoss`). `StandardEnemyFactory` creates the standard enemy family (`FlyEnemy`, `GaperEnemy`, `SpitterEnemy`, `ChargerEnemy`, `SpiderEnemy`, `MonstroEnemy`), while `BossRushEnemyFactory` creates the boss-rush family (`BossRushFlyEnemy`, `BossRushGaperEnemy`, `BossRushSpitterEnemy`, `BossRushSpiderEnemy`, `BossRushMonstroEnemy`). |
| **Factory** | `Game/Entities/PickupFactory.cs`, `Game/Entities/Pickup.cs` | `PickupFactory` centralizes pickup creation and returns the base `Pickup` type while creating at least three concrete product classes: `HeartPickup`, `ItemPickup` and `TrapdoorPickup`. |
| **Prototype** | `Core/IPrototype.cs`, `Game/Characters/`, `Game/Items/Stats.cs` | Each playable character (Isaac, Magdalene, Cain, Judas) is a configured `Character` prototype in a `CharacterRegistry`. Every player gets a deep clone of one, which becomes their own stats and item list. `dotnet run -- --demo prototype` compares shallow and deep copies with memory addresses. |
| **Bridge** | `Rendering/` | HUD widgets (`HudWidget` Abstraction, refined by `MinimapWidget` and `BossBarWidget`) decide what to show and hand every shape to an `ICanvas` (Implementor). `RaylibCanvas` draws into the game window and `SvgCanvas` writes an SVG image, so widgets and devices can be added independently. `dotnet run -- --demo bridge` draws the same widgets on both canvases. |
| **Facade** | `Game/Dungeon/DungeonFacade.cs` | `DungeonFacade` is the one entry point to the dungeon subsystems (`FloorDirector`, `StandardFloorBuilder`, `BossRushFloorBuilder`, the floor's `IEnemyFactory`, `PickupFactory`). Its clients `World` and `LootDropper` only call `CreateFloor`, `SpawnEnemies` and `DropClearReward`. `dotnet run -- --demo facade` shows each call and both clients. |
| **Strategy** | `Game/AI/`, `Game/Entities/Enemy.cs` | `Enemy` (Context) delegates to an `IMovementStrategy` (`ChaseMovement`, `BuzzMovement`, `KeepDistanceMovement`, `TakeCoverMovement`, `ChargeMovement`, `BounceMovement`, `JumpMovement`, `SkitterMovement`) and a list of `IAttackStrategy` (`AimedShotAttack`, `LandingRingAttack`, `SpitSprayAttack`). Each enemy subclass picks its strategies in its constructor, and `SetBehaviour` swaps them at runtime: Monstro does this when it drops below half health to enter its enraged phase. |
| **Decorator** | `Game/Items/Stats.cs`, `Game/Items/ItemDecorators.cs` | `IStats` (Component) is all the game reads. `BaseStats` (Concrete Component) holds a character's stats before any items (each `Character` prototype sets its own). `StatsDecorator` (Decorator) wraps another `IStats` and passes every stat through; each item has a Concrete Decorator (`DamageUpDecorator`, `TearsUpDecorator`, `SpeedUpDecorator`, `RangeUpDecorator`, `HeartContainerDecorator`, `GlassCannonDecorator`) that overrides only the stats it changes. Picking up an item makes `Character.AddItem` wrap the stats in one more layer (`ItemCatalog.Decorate`), so pickup order matters: Glass Cannon doubles all the damage collected before it. |
| **Command** | `Game/Commands/`, `DevTools/DevConsole.cs` | Each undoable cheat of the F6 console is a command object: `ICommand` (Command) has `Execute()` and `Undo()`, and `GiveItemCommand`, `HealCommand`, `ToggleCheatCommand` and `RevealMapCommand` (Concrete Commands) save what they change just before changing it, then call their Receiver, the `World` or a `Player`. `CommandHistory` (Invoker) runs them and keeps undo and redo stacks without knowing what any command does. `DevConsole` (Client) creates the commands from its buttons and offers Undo/Redo (Ctrl+Z, Ctrl+Y). `dotnet run -- --demo command` executes, undoes and redoes a sequence of cheats. |

The earlier implementations of State, Command, Factory, Strategy, Composite and Object Pool are in
`old-patterns/` for reference. That folder is not compiled.

Plain code that other patterns could replace:

| Code | Pattern that would fit |
|---|---|
| `IsaacGame` switches between the menu and the game with `if (_session == null)` | State |
| `Enemy` runs a plain list of attack strategies | Composite (one `IAttackStrategy` made of several) |
| `World.SpawnTear` allocates a new `Tear` for every shot | Object Pool |
| `HostSession` / `ClientSession` behind `IGameSession` | Proxy |

## Project layout

```
Core/        config, event bus, logger and the prototype interface
Demos/       console demonstrations of the patterns (--demo)
DevTools/    F6 developer console: cheats and the pattern checklist
Game/        World simulation, player input, events and listeners
  AI/        enemy movement and attack strategies, pathfinding around rocks
  Commands/  undoable dev console cheats (command pattern)
  Characters/ character prototypes and their registry
  Dungeon/   rooms, floor director, the two floor builders and the dungeon facade
  Entities/  player, enemies (with their AI), tears, pickups
  Items/     player stats, item decorators and item catalog
Net/         snapshot serialization, host and client sessions
Rendering/   shape-based renderer (no assets needed), HUD widgets and their canvases
IsaacGame.cs window and main loop, switches between menu and game
Menu.cs      main menu
```
