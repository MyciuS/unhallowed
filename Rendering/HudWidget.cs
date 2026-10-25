using System.Drawing;
using Isaac.Net;

namespace Isaac.Rendering;

/// <summary>
/// BRIDGE pattern, the Abstraction: a part of the HUD that knows what to show from a
/// <see cref="Snapshot"/> but not how it gets drawn. It passes every shape to its
/// <see cref="ICanvas"/>, so new widgets and new canvases can be added independently and
/// combined freely.
/// </summary>
public abstract class HudWidget
{
    protected HudWidget(ICanvas canvas, int x, int y)
    {
        Canvas = canvas;
        X = x;
        Y = y;
    }

    /// <summary>Top-left corner of the widget on its canvas.</summary>
    public int X { get; }
    public int Y { get; }

    protected ICanvas Canvas { get; }

    public abstract void Draw(Snapshot snapshot);

    protected void DrawCenteredText(string text, int centerX, int y, int size, Color color, bool shadow = false)
    {
        int x = centerX - Canvas.MeasureText(text, size) / 2;
        if (shadow) Canvas.DrawText(text, x + 2, y + 2, size, Color.Black);
        Canvas.DrawText(text, x, y, size, color);
    }
}
