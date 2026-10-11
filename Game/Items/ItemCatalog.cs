namespace Isaac.Game.Items;

public enum ItemKind : byte
{
    DamageUp,
    TearsUp,
    SpeedUp,
    RangeUp,
    HeartContainer,
    GlassCannon,
}

/// <summary>Item list and display names. The stat changes live in <see cref="Stats.Apply"/>.</summary>
public static class ItemCatalog
{
    public static readonly ItemKind[] All = Enum.GetValues<ItemKind>();

    public static string Name(ItemKind kind) => kind switch
    {
        ItemKind.DamageUp => "Damage Up",
        ItemKind.TearsUp => "Tears Up",
        ItemKind.SpeedUp => "Speed Up",
        ItemKind.RangeUp => "Range Up",
        ItemKind.HeartContainer => "Heart Container",
        ItemKind.GlassCannon => "Glass Cannon",
        _ => kind.ToString(),
    };
}
