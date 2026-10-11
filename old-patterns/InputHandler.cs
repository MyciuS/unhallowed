using System.Numerics;
using Raylib_cs;

namespace Isaac.Game;

/// <summary>
/// Turns raw keyboard state into <see cref="ICommand"/>s (COMMAND pattern).
/// Bindings are data, so they can be remapped without touching game logic.
/// </summary>
public sealed class InputHandler
{
    private readonly Dictionary<KeyboardKey, Vector2> _moveBindings = new()
    {
        [KeyboardKey.W] = -Vector2.UnitY,
        [KeyboardKey.S] = Vector2.UnitY,
        [KeyboardKey.A] = -Vector2.UnitX,
        [KeyboardKey.D] = Vector2.UnitX,
    };

    private readonly Dictionary<KeyboardKey, Vector2> _shootBindings = new()
    {
        [KeyboardKey.Up] = -Vector2.UnitY,
        [KeyboardKey.Down] = Vector2.UnitY,
        [KeyboardKey.Left] = -Vector2.UnitX,
        [KeyboardKey.Right] = Vector2.UnitX,
    };

    public void BindMove(KeyboardKey key, Vector2 dir) => _moveBindings[key] = dir;
    public void BindShoot(KeyboardKey key, Vector2 dir) => _shootBindings[key] = dir;

    /// <summary>Creates this frame's commands. Both are always sent so releasing keys stops the player.</summary>
    public IReadOnlyList<ICommand> Poll() =>
    [
        new MoveCommand(Sum(_moveBindings)),
        new ShootCommand(Sum(_shootBindings)),
    ];

    private static Vector2 Sum(Dictionary<KeyboardKey, Vector2> bindings)
    {
        var v = Vector2.Zero;
        foreach (var (key, dir) in bindings)
            if (Raylib.IsKeyDown(key)) v += dir;
        return v;
    }
}
