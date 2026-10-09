using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;

namespace Isaac.Game;

// Events published on the World's EventBus (OBSERVER pattern).
public readonly record struct EnemyKilled(Enemy Enemy);
public readonly record struct PlayerHurt(Player Player);
public readonly record struct PlayerDied(Player Player);
public readonly record struct RoomCleared(Room Room, Floor Floor);
public readonly record struct ItemPickedUp(Player Player, ItemKind Item);
public readonly record struct FloorStarted(Floor Floor);
