using Raylib_cs;
using Color = System.Drawing.Color;

namespace Isaac.Rendering;

/// <summary>BRIDGE pattern, a Concrete Implementor: draws into the game window through Raylib.</summary>
public sealed class RaylibCanvas : ICanvas
{
    public void FillRect(int x, int y, int width, int height, Color color) =>
        Raylib.DrawRectangle(x, y, width, height, ToRaylib(color));

    public void DrawText(string text, int x, int y, int size, Color color) =>
        Raylib.DrawText(text, x, y, size, ToRaylib(color));

    public int MeasureText(string text, int size) => Raylib.MeasureText(text, size);

    private static Raylib_cs.Color ToRaylib(Color c) => new(c.R, c.G, c.B, c.A);
}
