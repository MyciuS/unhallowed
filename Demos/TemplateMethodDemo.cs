using Isaac.Game;
using Isaac.Game.AI;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;

namespace Isaac.Demos;

/// <summary>
/// TEMPLATE METHOD pattern demo. Ticks Monstro's two cooldown attacks side by side, first with a
/// rock between him and the player, then with the rock gone. Both run the same
/// <see cref="CooldownAttack.Attack"/> skeleton, but only <see cref="AimedShotAttack"/> overrides the
/// <c>CanFire</c> hook, so only it holds its fire while the rock blocks the shot.
/// </summary>
public static class TemplateMethodDemo
{
    private const float Dt = 0.25f;
    private const int Rock = 6; // tile column between the two, on the middle row

    public static void Run()
    {
        Console.WriteLine("TEMPLATE METHOD: CooldownAttack.Attack() fixes the steps, subclasses fill them in");
        Console.WriteLine();
        Console.WriteLine("  Abstract class    CooldownAttack");
        Console.WriteLine("  Template method   Attack(): timer -= dt; if ready and CanFire(): restart timer, Fire()");
        Console.WriteLine("  Hook              CanFire()  (default: always true)");
        Console.WriteLine("  Primitive op      Fire()     (abstract)");
        Console.WriteLine("  Concrete classes  AimedShotAttack  (sealed) overrides CanFire() and Fire()");
        Console.WriteLine("                    SpitSprayAttack  (sealed) overrides Fire() only");

        var world = new World();
        var player = world.AddPlayer(0);
        var monstro = new MonstroEnemy(Room.TileCenter(3, 3), 1);
        player.Pos = Room.TileCenter(9, 3);
        var aimed = new AimedShotAttack(1.1f, 360f);
        var spray = new SpitSprayAttack(count: 10, cooldown: 1.6f, spreadDegrees: 25f, minSpeed: 200f, maxSpeed: 360f);

        Console.WriteLine();
        Console.WriteLine($"== Rock between Monstro and the player: line of sight = {SetRock(world, true, monstro, player)} ==");
        float t = 0;
        for (; t < 3f; t += Dt)
            Tick(world, monstro, player, aimed, spray, t);

        Console.WriteLine();
        Console.WriteLine($"== Rock removed: line of sight = {SetRock(world, false, monstro, player)} ==");
        for (; t < 4.5f; t += Dt)
            Tick(world, monstro, player, aimed, spray, t);

        Console.WriteLine();
        Console.WriteLine("  The timer, the null-target check and the restart are written once, in CooldownAttack.");
        Console.WriteLine("  A new cooldown attack only has to write Fire(), and CanFire() if it needs a condition.");
    }

    private static bool SetRock(World world, bool rock, Enemy monstro, Player player)
    {
        world.Room.Rocks[Rock, 3] = rock;
        return world.HasLineOfSight(monstro.Pos, player.Pos);
    }

    private static void Tick(World world, Enemy monstro, Player player, CooldownAttack aimed, CooldownAttack spray, float t)
    {
        Console.WriteLine(
            $"  t {t + Dt,4:0.00}s  AimedShotAttack {Step(world, monstro, player, aimed),-28}" +
            $"  SpitSprayAttack {Step(world, monstro, player, spray)}");
    }

    /// <summary>Runs one Attack() call and says which step of the template it stopped at.</summary>
    private static string Step(World world, Enemy monstro, Player player, CooldownAttack attack)
    {
        int before = world.Tears.Count;
        attack.Attack(monstro, player, world, Dt);
        int fired = world.Tears.Count - before;
        if (fired > 0)
            return $"Fire(): {fired} tear{(fired == 1 ? "" : "s")}";
        return attack.TimeLeft > 0 ? $"cooling down, {attack.TimeLeft:0.00}s left" : "ready, CanFire() = false";
    }
}
