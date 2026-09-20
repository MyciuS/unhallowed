using System.Drawing;
using Unhallowed.Core.Rendering;

namespace Unhallowed.Client.Rendering;

/// <summary>Maps the domain's abstract <see cref="PaletteColor"/> onto real GDI+ colours.</summary>
internal static class Palette
{
    private static readonly Dictionary<PaletteColor, Color> _colors = new()
    {
        [PaletteColor.Background] = Color.FromArgb(18, 16, 22),
        [PaletteColor.Wall] = Color.FromArgb(52, 46, 60),
        [PaletteColor.Floor] = Color.FromArgb(34, 30, 40),
        [PaletteColor.Player1] = Color.FromArgb(236, 226, 210),
        [PaletteColor.Player2] = Color.FromArgb(120, 190, 255),
        [PaletteColor.Player3] = Color.FromArgb(150, 235, 160),
        [PaletteColor.Player4] = Color.FromArgb(255, 190, 120),
        [PaletteColor.Enemy] = Color.FromArgb(200, 70, 80),
        [PaletteColor.Boss] = Color.FromArgb(150, 40, 120),
        [PaletteColor.Projectile] = Color.FromArgb(245, 245, 235),
        [PaletteColor.Pickup] = Color.FromArgb(240, 210, 90),
        [PaletteColor.HudText] = Color.FromArgb(220, 216, 210),
        [PaletteColor.HealthFull] = Color.FromArgb(215, 60, 70),
        [PaletteColor.HealthEmpty] = Color.FromArgb(70, 58, 62),
    };

    public static Color ToGdi(PaletteColor color)
        => _colors.TryGetValue(color, out var found) ? found : Color.Magenta;

    /// <summary>Chooses a palette entry from the snapshot's <c>Visual</c> discriminator.</summary>
    public static PaletteColor ForVisual(string visual) => visual switch
    {
        "player0" => PaletteColor.Player1,
        "player1" => PaletteColor.Player2,
        "player2" => PaletteColor.Player3,
        "player3" => PaletteColor.Player4,
        "monstro" => PaletteColor.Boss,
        "projectile" => PaletteColor.Projectile,
        "pickup" => PaletteColor.Pickup,
        _ => PaletteColor.Enemy,
    };
}
