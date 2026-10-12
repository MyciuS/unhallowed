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

**Controls:** WASD to move, arrow keys to shoot, Esc to go back to the menu, and R to restart after a game over (host only).

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
 Renderer.Draw(snapshot) <--(SignalR)--     World.BuildSnapshot() -> Snapshot.Write()
```

- Clients never run game logic. They send their input and draw the newest `Snapshot`.
- The host draws exactly the same `Snapshot` object, so host and clients share one render path.
- The host runs an embedded ASP.NET Core server with a SignalR hub at `/game`. Hub calls arrive on
  thread pool threads, so `GameHub` only queues them and `HostSession` applies them on the game thread.
- Input and snapshots are packed into small binary blobs (`BinaryWriter`) and sent with the
  MessagePack hub protocol, so they aren't base64-encoded inside JSON.
- SignalR runs over TCP and never drops data. If a client is still receiving the previous snapshot, the host
  skips sending it new ones until that finishes, so a slow client gets the newest state rather than a backlog.

## Design patterns

Only six patterns are implemented for now. The rest of the code is intentionally plain, so other
team members can add their own patterns.

| Pattern | Where | What it does |
|---|---|---|
| **Singleton** | `Core/GameLogger.cs` | One thread-safe log for the whole program, reached through `GameLogger.Instance`. A private constructor and `Lazy<T>` make sure only one instance is created, and a lock keeps the game thread and SignalR threads from mixing up lines. Writes to the console and `game.log`. |
| **Observer** | `Core/EventBus.cs`, `Game/Events.cs`, `Game/Listeners.cs` | `ScoreKeeper`, `Announcer`, `LootDropper` and `ActionLogger` react to world events without being coupled to the code that raises them |
| **Builder** | `Game/Dungeon/` | `FloorDirector` (Director) runs the same steps (`Reset`, `BuildLayout`, `AssignRoomKinds`, `AddObstacles`, `AddEnemies`, `AddRewards`, `GetResult`) on an `IFloorBuilder` (Builder). `StandardFloorBuilder` and `BossRushFloorBuilder` (Concrete Builders) produce a `Floor` (Product). `World` (Client) picks the builder for each floor. |
| **Abstract Factory** | `Game/Entities/EnemyFactory.cs`, `Game/Entities/Enemy.cs` | `IEnemyFactory` defines creation methods for a family of related enemies (`CreateSwarmEnemy`, `CreateChaserEnemy`, `CreateShooterEnemy`, `CreateRusherEnemy`, `CreateSkitterEnemy`, `CreateBoss`). `StandardEnemyFactory` creates the standard enemy family (`FlyEnemy`, `GaperEnemy`, `SpitterEnemy`, `ChargerEnemy`, `SpiderEnemy`, `MonstroEnemy`), while `BossRushEnemyFactory` creates the boss-rush family (`BossRushFlyEnemy`, `BossRushGaperEnemy`, `BossRushSpitterEnemy`, `BossRushSpiderEnemy`, `BossRushMonstroEnemy`). |
| **Factory** | `Game/Entities/PickupFactory.cs`, `Game/Entities/Pickup.cs` | `PickupFactory` centralizes pickup creation and returns the base `Pickup` type while creating at least three concrete product classes: `HeartPickup`, `ItemPickup` and `TrapdoorPickup`. |
| **Strategy** | `Game/AI/`, `Game/Entities/Enemy.cs` | `Enemy` (Context) delegates to an `IMovementStrategy` (`ChaseMovement`, `BuzzMovement`, `KeepDistanceMovement`, `TakeCoverMovement`, `ChargeMovement`, `BounceMovement`, `JumpMovement`, `SkitterMovement`) and a list of `IAttackStrategy` (`AimedShotAttack`, `LandingRingAttack`, `SpitSprayAttack`). Each enemy subclass picks its strategies in its constructor, and `SetBehaviour` swaps them at runtime: Monstro does this when it drops below half health to enter its enraged phase. |

The earlier implementations of State, Command, Factory, Strategy, Composite and Object Pool are in
`old-patterns/` for reference. That folder is not compiled.

Plain code that other patterns could replace:

| Code | Pattern that would fit |
|---|---|
| `IsaacGame` switches between the menu and the game with `if (_session == null)` | State |
| `PlayerInput` is a plain struct of move and shoot directions | Command |
| `Enemy` runs a plain list of attack strategies | Composite (one `IAttackStrategy` made of several) |
| `Stats.Apply` changes the numbers directly when an item is picked up | Decorator |
| `World.SpawnTear` allocates a new `Tear` for every shot | Object Pool |
| `HostSession` / `ClientSession` behind `IGameSession` | Proxy |

## Project layout

```
Core/        config, event bus and logger
Game/        World simulation, player input, events and listeners
  AI/        enemy movement and attack strategies, pathfinding around rocks
  Dungeon/   rooms, floor director and the two floor builders
  Entities/  player, enemies (with their AI), tears, pickups
  Items/     player stats and item catalog
Net/         snapshot serialization, host and client sessions
Rendering/   shape-based renderer (no assets needed)
IsaacGame.cs window and main loop, switches between menu and game
Menu.cs      main menu
```
