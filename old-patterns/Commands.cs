using System.Numerics;
using Isaac.Game.Entities;

namespace Isaac.Game;

public enum CommandType : byte
{
    Move,
    Shoot,
}

/// <summary>
/// COMMAND pattern: player input is turned into command objects. Commands are
/// serializable, so the same objects are executed locally (host) or sent over
/// the network and executed on the host for remote players.
/// </summary>
public interface ICommand
{
    CommandType Type { get; }
    void Execute(Player player);
    void Serialize(BinaryWriter writer);
}

public sealed class MoveCommand(Vector2 direction) : ICommand
{
    public Vector2 Direction { get; } = direction;
    public CommandType Type => CommandType.Move;
    public void Execute(Player player) => player.MoveIntent = Direction;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write((byte)Type);
        writer.Write((sbyte)MathF.Sign(Direction.X));
        writer.Write((sbyte)MathF.Sign(Direction.Y));
    }
}

public sealed class ShootCommand(Vector2 direction) : ICommand
{
    public Vector2 Direction { get; } = direction;
    public CommandType Type => CommandType.Shoot;
    public void Execute(Player player) => player.ShootIntent = Direction;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write((byte)Type);
        writer.Write((sbyte)MathF.Sign(Direction.X));
        writer.Write((sbyte)MathF.Sign(Direction.Y));
    }
}

/// <summary>FACTORY: rebuilds the right command object from bytes received over the network.</summary>
public static class CommandFactory
{
    public static ICommand Deserialize(BinaryReader reader)
    {
        var type = (CommandType)reader.ReadByte();
        var dir = new Vector2(reader.ReadSByte(), reader.ReadSByte());
        return type switch
        {
            CommandType.Move => new MoveCommand(dir),
            CommandType.Shoot => new ShootCommand(dir),
            _ => throw new InvalidDataException($"Unknown command type {type}"),
        };
    }
}
