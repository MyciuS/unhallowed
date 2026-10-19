using System.Numerics;
using Isaac.Game.Items;

namespace Isaac.Game.Entities;

/// <summary>
/// FACTORY pattern: centralizes pickup creation so callers ask for the reward
/// they want without repeating constructor details.
/// </summary>
public static class PickupFactory
{
    public static Pickup CreateHeart(Vector2 pos) => new HeartPickup(pos);

    public static Pickup CreateTrapdoor(Vector2 pos) => new TrapdoorPickup(pos);

    public static Pickup CreateItemPedestal(Vector2 pos, ItemKind item) =>
        new ItemPickup(pos, item);

    public static Pickup CreateRandomItemPedestal(Vector2 pos, Random rng)
    {
        var item = ItemCatalog.All[rng.Next(ItemCatalog.All.Length)];
        return CreateItemPedestal(pos, item);
    }
}
