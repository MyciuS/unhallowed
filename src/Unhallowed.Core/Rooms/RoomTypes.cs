using Unhallowed.Core.AI;
using Unhallowed.Core.Common;
using Unhallowed.Core.Entities;
using Unhallowed.Core.Events;

namespace Unhallowed.Core.Rooms;

// PATTERN: Template Method (concrete classes) -> docs/patterns/22-template-method.md
//
// NOTE FOR THE TEAM: Populate() below news up enemies directly. That is deliberately the
// "before" state for the Factory Method / Abstract Factory reports - see
// docs/patterns/02-factory-method.md. Whoever owns those patterns refactors this call site.

/// <summary>Ordinary combat room. A handful of mixed enemies, no reward.</summary>
public sealed class NormalRoom(string id, Vec2 size, int seed) : RoomBase(id, size)
{
    public override string RoomType => "Normal";

    protected override void Populate(IWorldView world)
    {
        var random = new Random(seed);
        var count = 3 + random.Next(3);

        for (var i = 0; i < count; i++)
        {
            var position = new Vec2(
                80f + (random.NextSingle() * (Size.X - 160f)),
                80f + (random.NextSingle() * (Size.Y - 160f)));

            IMovementStrategy strategy = random.Next(3) switch
            {
                0 => new ChaserStrategy(),
                1 => new WandererStrategy(seed + i),
                _ => new ChargerStrategy(),
            };

            var archetype = strategy switch
            {
                ChaserStrategy => "gaper",
                WandererStrategy => "fly",
                _ => "charger",
            };

            world.Spawn(new Enemy(world.NextEntityId(), archetype, position, 8, 1, strategy));
        }
    }
}

/// <summary>Boss room. One large, tough enemy and a guaranteed item on clear.</summary>
public sealed class BossRoom(string id, Vec2 size) : RoomBase(id, size)
{
    public override string RoomType => "Boss";

    protected override void OnBeforePopulate(IWorldView world)
        => world.Events.Notify(new GameEvent(GameEventType.BossSpawned, 0, "A boss stirs..."));

    protected override void Populate(IWorldView world)
    {
        var centre = new Vec2(Size.X / 2f, Size.Y / 3f);
        world.Spawn(new Enemy(
            world.NextEntityId(), "monstro", centre, 60, 2, new ChargerStrategy(180f), radius: 30f));
    }

    protected override void GrantReward(IWorldView world)
        => world.Events.Notify(new GameEvent(GameEventType.RoomCleared, 0, "The boss drops an item"));
}

/// <summary>Treasure room. No enemies, so the doors never seal.</summary>
public sealed class TreasureRoom(string id, Vec2 size) : RoomBase(id, size)
{
    public override string RoomType => "Treasure";

    protected override bool RequiresClearing => false;

    protected override void Populate(IWorldView world)
    {
        // Pedestal item pickup goes here once the Prototype registry lands
        // (docs/patterns/05-prototype.md).
    }
}
