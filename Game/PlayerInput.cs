using System.Numerics;
using Isaac.Game.Entities;
using Raylib_cs;

namespace Isaac.Game;

/// <summary>
/// What a player wants to do this frame: a move and a shoot direction.
/// The host applies its own input directly; clients send theirs every frame.
/// </summary>
public readonly record struct PlayerInput(Vector2 Move, Vector2 Shoot)
{
    /// <summary>WASD to move, arrow keys to shoot. Opposite keys cancel out.</summary>
    public static PlayerInput ReadKeyboard() => new(
        Axis(KeyboardKey.W, KeyboardKey.S, KeyboardKey.A, KeyboardKey.D),
        Axis(KeyboardKey.Up, KeyboardKey.Down, KeyboardKey.Left, KeyboardKey.Right));

    private static Vector2 Axis(KeyboardKey up, KeyboardKey down, KeyboardKey left, KeyboardKey right) => new(
        (Raylib.IsKeyDown(right) ? 1 : 0) - (Raylib.IsKeyDown(left) ? 1 : 0),
        (Raylib.IsKeyDown(down) ? 1 : 0) - (Raylib.IsKeyDown(up) ? 1 : 0));

    public void ApplyTo(Player player)
    {
        player.MoveIntent = Move;
        player.ShootIntent = Shoot;
    }

    /// <summary>
    /// Input from a client is untrusted JSON. Reduce each axis to -1, 0 or 1, the same values
    /// the keyboard produces, so a client can't move or shoot faster by sending bigger numbers.
    /// </summary>
    public PlayerInput Sanitized() => new(
        new Vector2(Sign(Move.X), Sign(Move.Y)),
        new Vector2(Sign(Shoot.X), Sign(Shoot.Y)));

    // Unlike MathF.Sign this does not throw on NaN.
    private static float Sign(float v) => v > 0 ? 1 : v < 0 ? -1 : 0;
}
