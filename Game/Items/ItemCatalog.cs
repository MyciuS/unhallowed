namespace Isaac.Game.Items;

public enum ItemKind : byte
{
    DamageUp,
    TearsUp,
    SpeedUp,
    RangeUp,
    HeartContainer,
    GlassCannon,
    GodMode, // demo only, never in the random pool
}

/// <summary>Item list and display names. The stat changes live in <see cref="Stats.Apply"/>.</summary>
public static class ItemCatalog
{
    /// <summary>Items that can show up in treasure rooms.</summary>
    public static readonly ItemKind[] All = Enum.GetValues<ItemKind>().Where(k => k != ItemKind.GodMode).ToArray();

    public static string Name(ItemKind kind) => kind switch
    {
        ItemKind.DamageUp => "Damage Up",
        ItemKind.TearsUp => "Tears Up",
        ItemKind.SpeedUp => "Speed Up",
        ItemKind.RangeUp => "Range Up",
        ItemKind.HeartContainer => "Heart Container",
        ItemKind.GlassCannon => "Glass Cannon",
        ItemKind.GodMode => "God Mode",
        _ => kind.ToString(),
    };
}
