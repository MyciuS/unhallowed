using System.Numerics;
using Isaac.Game.Items;

namespace Isaac.Game.Entities;

public enum PickupKind : byte
{
    Heart,
    Item,      // item pedestal
    Trapdoor,  // leads to the next floor
}

public sealed class Pickup(PickupKind kind, Vector2 pos, ItemKind item = default)
{
    public PickupKind Kind { get; } = kind;
    public Vector2 Pos { get; } = pos;
    public ItemKind Item { get; } = item;
    public float Radius => Kind == PickupKind.Trapdoor ? 28f : 16f;
}
