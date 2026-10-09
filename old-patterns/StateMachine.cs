namespace Isaac.Core;

/// <summary>STATE pattern: each screen of the game is a self-contained state.</summary>
public interface IGameState
{
    void Enter();
    void Update(float dt);
    void Draw();
    void Exit();
}

public sealed class StateMachine
{
    private IGameState? _pending;

    public IGameState? Current { get; private set; }

    /// <summary>The switch happens at the start of the next update so a state can safely replace itself.</summary>
    public void Change(IGameState next) => _pending = next;

    public void Update(float dt)
    {
        if (_pending != null)
        {
            Current?.Exit();
            Current = _pending;
            _pending = null;
            Current.Enter();
        }
        Current?.Update(dt);
    }

    public void Draw() => Current?.Draw();

    public void Shutdown()
    {
        Current?.Exit();
        Current = null;
    }
}
