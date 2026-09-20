using Unhallowed.Core.Common;
using Unhallowed.Core.Entities;

namespace Unhallowed.Core.AI;

// PATTERN: Strategy (concrete strategies) -> docs/patterns/21-strategy.md

/// <summary>Walks straight at the closest living player. The basic Isaac-style "fly".</summary>
public sealed class ChaserStrategy(float speed = 55f) : IMovementStrategy
{
    public string Name => "Chaser";

    public Vec2 ComputeVelocity(Entity self, IWorldView world, float deltaSeconds)
    {
        var target = world.FindNearestPlayer(self.Position);
        if (target is null)
        {
            return Vec2.Zero;
        }

        return (target.Position - self.Position).Normalized() * speed;
    }
}

/// <summary>Drifts randomly, changing heading on a timer. Used for filler enemies.</summary>
public sealed class WandererStrategy(int seed, float speed = 40f) : IMovementStrategy
{
    private readonly Random _random = new(seed);
    private Vec2 _heading = Vec2.Zero;
    private float _timeUntilTurn;

    public string Name => "Wanderer";

    public Vec2 ComputeVelocity(Entity self, IWorldView world, float deltaSeconds)
    {
        _timeUntilTurn -= deltaSeconds;
        if (_timeUntilTurn <= 0f)
        {
            var angle = _random.NextSingle() * MathF.Tau;
            _heading = new Vec2(MathF.Cos(angle), MathF.Sin(angle));
            _timeUntilTurn = 0.8f + (_random.NextSingle() * 1.2f);
        }

        return _heading * speed;
    }
}

/// <summary>
/// Builds up, then dashes at the player's last known position and recovers.
/// Shows that a strategy may carry its own state without the enemy knowing about it.
/// </summary>
public sealed class ChargerStrategy(float chargeSpeed = 220f) : IMovementStrategy
{
    private Vec2 _lockedDirection = Vec2.Zero;
    private float _windUp = 1.0f;
    private float _dashRemaining;

    public string Name => "Charger";

    public Vec2 ComputeVelocity(Entity self, IWorldView world, float deltaSeconds)
    {
        if (_dashRemaining > 0f)
        {
            _dashRemaining -= deltaSeconds;
            return _lockedDirection * chargeSpeed;
        }

        _windUp -= deltaSeconds;
        if (_windUp > 0f)
        {
            return Vec2.Zero;
        }

        var target = world.FindNearestPlayer(self.Position);
        if (target is null)
        {
            return Vec2.Zero;
        }

        _lockedDirection = (target.Position - self.Position).Normalized();
        _dashRemaining = 0.45f;
        _windUp = 1.4f;
        return _lockedDirection * chargeSpeed;
    }
}

/// <summary>Never moves. Turrets and stationary bosses use this.</summary>
public sealed class StationaryStrategy : IMovementStrategy
{
    public string Name => "Stationary";

    public Vec2 ComputeVelocity(Entity self, IWorldView world, float deltaSeconds) => Vec2.Zero;
}
