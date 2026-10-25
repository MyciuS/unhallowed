using System.Drawing;

namespace Isaac.Rendering;

/// <summary>
/// BRIDGE pattern, the Implementor: the few drawing primitives a <see cref="HudWidget"/> is built
/// from. Each implementation puts them on a different device, so any widget can be shown on any
/// device without the widget knowing which one it is.
/// </summary>
public interface ICanvas
{
    void FillRect(int x, int y, int width, int height, Color color);

    /// <summary>Draws <paramref name="text"/> with its top-left corner at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    void DrawText(string text, int x, int y, int size, Color color);

    /// <summary>Width of <paramref name="text"/> in pixels, so a widget can centre it on any device.</summary>
    int MeasureText(string text, int size);
}
