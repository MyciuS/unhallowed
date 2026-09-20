using Unhallowed.Core.Entities;
using Unhallowed.Core.Events;

namespace Unhallowed.Core.States;

// PATTERN: State (concrete states) -> docs/patterns/20-state.md

/// <summary>Standing still. Transitions to <see cref="MovingState"/> as soon as input arrives.</summary>
public sealed class IdleState : IPlayerState
{
    public string Name => "Idle";

    public bool IsInvulnerable => false;

    public bool AcceptsInput => true;

    public void Enter(Player player) => player.Velocity = Common.Vec2.Zero;

    public IPlayerState Update(Player player, float deltaSeconds, IWorldView world)
    {
        if (!player.IsAlive)
        {
            return new DeadState();
        }

        return player.MoveIntent.Length > 0.01f ? new MovingState() : this;
    }

    public void Exit(Player player)
    {
    }
}

/// <summary>Walking. Falls back to <see cref="IdleState"/> when the stick is released.</summary>
public sealed class MovingState : IPlayerState
{
    public string Name => "Moving";

    public bool IsInvulnerable => false;

    public bool AcceptsInput => true;

    public void Enter(Player player)
    {
    }

    public IPlayerState Update(Player player, float deltaSeconds, IWorldView world)
    {
        if (!player.IsAlive)
        {
            return new DeadState();
        }

        if (player.MoveIntent.Length <= 0.01f)
        {
            return new IdleState();
        }

        player.Velocity = player.MoveIntent.Normalized() * player.Stats.MoveSpeed;
        return this;
    }

    public void Exit(Player player)
    {
    }
}

/// <summary>Post-hit invulnerability window. Ignores damage, then returns control.</summary>
public sealed class HurtState(float durationSeconds = 0.9f) : IPlayerState
{
    private float _remaining = durationSeconds;

    public string Name => "Hurt";

    public bool IsInvulnerable => true;

    public bool AcceptsInput => true;

    public void Enter(Player player)
    {
    }

    public IPlayerState Update(Player player, float deltaSeconds, IWorldView world)
    {
        if (!player.IsAlive)
        {
            return new DeadState();
        }

        _remaining -= deltaSeconds;
        if (_remaining > 0f)
        {
            player.Velocity = player.MoveIntent.Normalized() * player.Stats.MoveSpeed;
            return this;
        }

        return new IdleState();
    }

    public void Exit(Player player)
    {
    }
}

/// <summary>Downed. Absorbing no input and no damage until a co-op revive lands.</summary>
public sealed class DeadState : IPlayerState
{
    public string Name => "Dead";

    public bool IsInvulnerable => true;

    public bool AcceptsInput => false;

    public void Enter(Player player)
    {
        player.Velocity = Common.Vec2.Zero;
        player.World?.Events.Notify(
            new GameEvent(GameEventType.PlayerDied, player.Id, $"{player.DisplayName} is down!"));
    }

    public IPlayerState Update(Player player, float deltaSeconds, IWorldView world)
        => player.IsAlive ? new IdleState() : this;

    public void Exit(Player player)
    {
    }
}
