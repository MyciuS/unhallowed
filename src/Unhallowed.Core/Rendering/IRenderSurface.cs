namespace Unhallowed.Core.Rendering;

// PATTERN: Adapter (target interface) -> docs/patterns/06-adapter.md
// Core must not reference System.Drawing: the server project runs the same simulation code and
// has no UI at all. So the domain talks to this small target interface, and the WinForms client
// supplies GdiRenderSurface, an adapter over System.Drawing.Graphics.

/// <summary>Colours the renderer understands, kept abstract so Core never names a UI type.</summary>
public enum PaletteColor
{
    Background,
    Wall,
    Floor,
    Player1,
    Player2,
    Player3,
    Player4,
    Enemy,
    Boss,
    Projectile,
    Pickup,
    HudText,
    HealthFull,
    HealthEmpty,
}

/// <summary>
/// The primitive drawing surface the game renders through - requirement 8 (primitive graphics).
/// Implemented by the client as an adapter over <c>System.Drawing.Graphics</c>.
/// </summary>
public interface IRenderSurface
{
    int Width { get; }

    int Height { get; }

    void Clear(PaletteColor color);

    void FillCircle(float centerX, float centerY, float radius, PaletteColor color);

    void FillRectangle(float x, float y, float width, float height, PaletteColor color);

    void DrawRectangle(float x, float y, float width, float height, PaletteColor color, float thickness = 1f);

    void DrawLine(float x1, float y1, float x2, float y2, PaletteColor color, float thickness = 1f);

    void DrawText(string text, float x, float y, PaletteColor color, float size = 12f);
}
