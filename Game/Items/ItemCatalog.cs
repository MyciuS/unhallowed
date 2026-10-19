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

/// <summary>Item list, display names, and which stats decorator each item adds.</summary>
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

    /// <summary>Wraps <paramref name="stats"/> in the decorator for <paramref name="kind"/>.</summary>
    public static IStats Decorate(ItemKind kind, IStats stats) => kind switch
    {
        ItemKind.DamageUp => new DamageUpDecorator(stats),
        ItemKind.TearsUp => new TearsUpDecorator(stats),
        ItemKind.SpeedUp => new SpeedUpDecorator(stats),
        ItemKind.RangeUp => new RangeUpDecorator(stats),
        ItemKind.HeartContainer => new HeartContainerDecorator(stats),
        ItemKind.GlassCannon => new GlassCannonDecorator(stats),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
