namespace Isaac.Game.Commands;

/// <summary>
/// COMMAND pattern, the Command interface: one action wrapped in an object, so it can be run
/// later, kept in a history and reversed. Each command remembers just enough of the state it
/// changes to put it back in <see cref="Undo"/>.
/// </summary>
public interface ICommand
{
    /// <summary>Short text for the history list, e.g. "Give Damage Up to P1".</summary>
    string Name { get; }

    void Execute();

    /// <summary>Reverses <see cref="Execute"/>. Only called on a command that was executed.</summary>
    void Undo();
}
