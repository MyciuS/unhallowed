namespace Isaac.Game.Commands;

/// <summary>
/// COMMAND pattern, the Invoker: runs commands and keeps them, so they can be undone and redone
/// in order. It only knows <see cref="ICommand"/>, never what a command actually does.
/// </summary>
public sealed class CommandHistory
{
    private readonly Stack<ICommand> _undo = new();
    private readonly Stack<ICommand> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Executed commands, the most recent first.</summary>
    public IEnumerable<ICommand> Done => _undo;

    public void Execute(ICommand command)
    {
        command.Execute();
        _undo.Push(command);
        // A new action starts a new branch of history: what was undone can no longer be redone.
        _redo.Clear();
    }

    /// <returns>The command that was undone, or null if there was nothing to undo.</returns>
    public ICommand? Undo()
    {
        if (!_undo.TryPop(out var command)) return null;
        command.Undo();
        _redo.Push(command);
        return command;
    }

    /// <returns>The command that was redone, or null if there was nothing to redo.</returns>
    public ICommand? Redo()
    {
        if (!_redo.TryPop(out var command)) return null;
        command.Execute();
        _undo.Push(command);
        return command;
    }

    /// <summary>Forgets everything, e.g. when the run restarts and the old players are gone.</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
