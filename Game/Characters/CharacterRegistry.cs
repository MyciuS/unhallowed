using Isaac.Core;
using Isaac.Game.Items;

namespace Isaac.Game.Characters;

/// <summary>
/// PROTOTYPE pattern, the prototype registry: holds one ready-made prototype per playable
/// character and hands out clones of them. Adding a character means registering one more
/// configured instance, not writing a new class.
/// </summary>
public sealed class CharacterRegistry
{
    private readonly List<Character> _prototypes = [];

    public int Count => _prototypes.Count;

    public void Register(Character prototype) => _prototypes.Add(prototype);

    public Character Create(string name) =>
        Clone(_prototypes.First(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Players get the characters in registration order: P1 the first, P2 the second, and so on.</summary>

    //public Character CreateForPlayer(int playerId) => Clone(_prototypes[playerId % _prototypes.Count]);
    public Character CreateForPlayer(int playerId) => Clone(_prototypes[0]);

    // The one place that decides how characters are copied, and it only knows the Prototype
    // interface, not the concrete class. A shallow copy here would let a player's item pickups
    // leak into the prototype and into every player cloned after them.
    private static Character Clone(IPrototype<Character> prototype) => prototype.ShallowClone();

    public static CharacterRegistry CreateDefault()
    {
        var registry = new CharacterRegistry();
        registry.Register(new Character("Isaac", new Stats()));
        registry.Register(new Character("Magdalene", new Stats { Speed = 220f, MaxHealth = 8 }));

        var cain = new Character("Cain", new Stats { Speed = 300f, Damage = 3f });
        cain.AddItem(ItemKind.RangeUp);
        registry.Register(cain);

        registry.Register(new Character("Judas", new Stats { Damage = 4.5f, MaxHealth = 4 }));
        return registry;
    }
}
