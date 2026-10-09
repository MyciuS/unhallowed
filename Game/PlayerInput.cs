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

    public void Write(BinaryWriter writer)
    {
        writer.Write((sbyte)MathF.Sign(Move.X));
        writer.Write((sbyte)MathF.Sign(Move.Y));
        writer.Write((sbyte)MathF.Sign(Shoot.X));
        writer.Write((sbyte)MathF.Sign(Shoot.Y));
    }

    public static PlayerInput Read(BinaryReader reader) => new(
        new Vector2(reader.ReadSByte(), reader.ReadSByte()),
        new Vector2(reader.ReadSByte(), reader.ReadSByte()));
}
