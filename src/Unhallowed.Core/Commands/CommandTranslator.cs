using Unhallowed.Core.Common;
using Unhallowed.Contracts.Messages;

namespace Unhallowed.Core.Commands;

/// <summary>
/// Turns a deserialized <see cref="PlayerCommandDto"/> back into a live <see cref="IGameCommand"/>.
/// Unknown types degrade to <see cref="NoOpCommand"/> so a malformed or newer client can never
/// crash the server loop.
/// </summary>
public static class CommandTranslator
{
    public static IGameCommand FromDto(PlayerCommandDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var direction = new Vec2(dto.X, dto.Y);
        return dto.Type switch
        {
            "Move" => new MoveCommand(dto.Sequence, direction),
            "Shoot" => new ShootCommand(dto.Sequence, direction),
            _ => new NoOpCommand(dto.Sequence),
        };
    }

    public static PlayerCommandDto ToDto(IGameCommand command, float x, float y)
        => new(command.Sequence, command.Type, x, y);
}
