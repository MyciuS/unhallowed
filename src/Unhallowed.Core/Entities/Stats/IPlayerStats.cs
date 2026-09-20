namespace Unhallowed.Core.Entities.Stats;

// PATTERN: Decorator (component) -> docs/patterns/09-decorator.md

/// <summary>
/// The stat block the simulation reads every tick. Items never write to a player's fields -
/// they wrap this component, so picking up ten items builds a ten-deep decorator chain.
/// </summary>
public interface IPlayerStats
{
    float Damage { get; }

    /// <summary>Pixels per second.</summary>
    float MoveSpeed { get; }

    /// <summary>Shots per second.</summary>
    float FireRate { get; }

    int MaxHealth { get; }

    /// <summary>Human-readable build, e.g. <c>Base + Damage Up + Speed Up</c>.</summary>
    string Describe();
}
