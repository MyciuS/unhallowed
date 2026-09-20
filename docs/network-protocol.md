# Network protocol

Requirement 6: client-server architecture over SignalR (WebSockets), payloads in JSON.

- **Hub path:** `/hub/match`
- **Default endpoint:** `http://localhost:5080/hub/match`
- **Serialisation:** SignalR JSON protocol, property names left as declared (PascalCase)
- **Constants:** `src/Unhallowed.Contracts/Protocol.cs` - change them there, both sides follow

## Joining

```
client                                   server
  |                                        |
  |--- JoinMatch(JoinMatchRequest) ------->|  creates the match if the code is new
  |                                        |  assigns a seat 0-3, spawns a Player
  |<-- MatchJoinedResponse ----------------|
  |                                        |
  |<-- PlayerJoined (to everyone else) ----|
  |                                        |
  |<-- Snapshot (15/s, from now on) -------|
```

`JoinMatch` throws a `HubException` if the match already holds 4 players.

### Request

```json
{ "MatchCode": "default", "DisplayName": "Ana" }
```

### Response

```json
{ "MatchCode": "default", "PlayerId": 1, "Seat": 0, "RoomId": "default-r1" }
```

`PlayerId` is the entity id to look for in snapshots - it is how the client knows which circle is
you. `Seat` (0-3) decides your colour.

## Client to server

| Method | Payload | Notes |
| --- | --- | --- |
| `JoinMatch` | `JoinMatchRequest` | Returns `MatchJoinedResponse`. |
| `SendCommand` | `PlayerCommandDto` | Fire and forget. Ignored if the caller is not in a match. |
| `LeaveMatch` | - | Also runs automatically on disconnect. |

### `PlayerCommandDto`

```json
{ "Sequence": 42, "Type": "Move", "X": 0.7, "Y": -0.7 }
```

| Field | Meaning |
| --- | --- |
| `Sequence` | Monotonic per client. The server drops anything at or below the last accepted value, which kills duplicates and out-of-order packets. |
| `Type` | `Move` or `Shoot`. Anything else becomes a no-op rather than an error. |
| `X`, `Y` | Direction, each in -1..1. Vectors longer than 1 are normalised server-side. |

Commands are **sticky**: the server keeps applying the last `Move` until a new one arrives. An
idle player sends nothing at all. Send a zero vector to stop.

Sending `Shoot` with a non-zero direction starts continuous fire at the player's fire rate; send
`{ "X": 0, "Y": 0 }` to stop.

## Server to client

| Method | Payload | Rate |
| --- | --- | --- |
| `Snapshot` | `WorldSnapshotDto` | 15/s, per connection |
| `GameEvent` | `GameEventDto` | as they happen |
| `PlayerJoined` | `PlayerPresenceDto` | on join |
| `PlayerLeft` | `PlayerPresenceDto` | on leave or disconnect |
| `MatchJoined` | reserved | currently returned directly from `JoinMatch` |
| `RoomChanged` | reserved | for room transitions |

### `WorldSnapshotDto`

```json
{
  "Tick": 1530,
  "RoomId": "default-r1",
  "AckSequence": 42,
  "Entities": [
    { "Id": 1, "Kind": "player",     "X": 480.0, "Y": 270.0, "Radius": 12.0,
      "Health": 6, "MaxHealth": 6, "Visual": "player0" },
    { "Id": 7, "Kind": "enemy",      "X": 120.5, "Y": 88.2,  "Radius": 14.0,
      "Health": 5, "MaxHealth": 8, "Visual": "gaper" },
    { "Id": 9, "Kind": "projectile", "X": 300.1, "Y": 200.4, "Radius": 5.0,
      "Health": 1, "MaxHealth": 1, "Visual": "projectile" }
  ]
}
```

Snapshots are **full, not delta-encoded**. With at most four players and a room's worth of
enemies this is a few kilobytes per second, and it keeps the protocol trivially debuggable - you
can read a snapshot and know the entire world state. Delta encoding would be a sensible
optimisation if entity counts ever grew.

`AckSequence` is per connection: it echoes the highest command sequence the server has accepted
from *that* client. It exists so a client implementing prediction can discard inputs the server
has already applied. Nothing consumes it yet.

`Visual` is the rendering discriminator (`player0`-`player3`, `gaper`, `fly`, `charger`,
`monstro`, `projectile`). `Kind` is the coarse category the client uses for draw ordering.

### `GameEventDto`

```json
{ "Type": "EnemyDied", "Message": "gaper died" }
```

Types come from `GameEventType`: `EnemyDied`, `PlayerDamaged`, `PlayerDied`, `ItemPickedUp`,
`RoomCleared`, `RoomEntered`, `BossSpawned`.

These are notifications for the HUD and the log. They carry no authoritative state - everything
that matters is already in the next snapshot. That separation is deliberate: a dropped event is
cosmetic, never a desync.

## Disconnects

`OnDisconnectedAsync` removes the player from the world and the group, tells the rest of the
match, and disposes the match once it is empty. The client has `WithAutomaticReconnect()`, but a
reconnect currently produces a **new** player rather than resuming the old one. Seat reservation
on reconnect is unimplemented and would be a reasonable feature branch.

## Testing without a client

The server exposes a status endpoint at `/`:

```bash
curl http://localhost:5080/
```

```json
{
  "service": "unhallowed-server",
  "hub": "/hub/match",
  "tickRate": 30,
  "snapshotRate": 15,
  "maxPlayers": 4,
  "activeMatches": [
    { "code": "default", "tick": 1530, "players": 2, "room": "default-r1" }
  ]
}
```

If `tick` is climbing, the simulation loop is alive.
