using Isaac.Core;
using Isaac.Game.Items;

namespace Isaac.Game.Characters;

/// <summary>
/// PROTOTYPE pattern, the Concrete Prototype: a playable character's starting loadout.
/// Characters differ only in data (stats and starting items), so instead of one subclass per
/// character, each character is a configured instance that new players are cloned from.
/// A clone becomes the player's own live loadout: picking up items changes its
/// <see cref="Stats"/> and <see cref="Items"/>, which is why a clone must not share them.
/// </summary>
public sealed class Character : IPrototype<Character>
{
    public Character(string name, Stats stats)
        : this(name, stats, new List<ItemKind>())
    {
    }

    private Character(string name, Stats stats, List<ItemKind> items)
    {
        Name = name;
        Stats = stats;
        Items = items;
    }

    public string Name { get; }
    public Stats Stats { get; }
    public List<ItemKind> Items { get; }

    public void AddItem(ItemKind item)
    {
        Stats.Apply(item);
        Items.Add(item);
    }

    /// <summary>Copies the fields as they are: the clone shares this character's Stats and Items.</summary>
    public Character ShallowClone() => (Character)MemberwiseClone();

    /// <summary>
    /// Also copies the Stats and the item list. The name is shared on purpose: strings are
    /// immutable, so no one can change it through either copy.
    /// </summary>
    public Character DeepClone() => new(Name, Stats.DeepClone(), new List<ItemKind>(Items));
}
