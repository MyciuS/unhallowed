using System.Text;
using Unhallowed.Core.Rendering;

namespace Unhallowed.PatternDemos.Demos;

/// <summary>
/// 06 - Adapter. The WinForms client adapts <c>System.Drawing.Graphics</c> to
/// <see cref="IRenderSurface"/>. To prove the domain really is decoupled from GDI+, this demo
/// supplies a completely different adaptee - the console - behind the same target interface.
/// </summary>
public sealed class AdapterDemo : IPatternDemo
{
    public string Key => "adapter";

    public int Number => 6;

    public string Title => "Adapter";

    public string Summary => "The same drawing calls hit GDI+ in the client and ASCII here.";

    /// <summary>
    /// Adapter whose adaptee is a character buffer instead of <c>Graphics</c>.
    /// Same target interface, wildly different backend, no change to any caller.
    /// </summary>
    private sealed class ConsoleRenderSurface(int width, int height) : IRenderSurface
    {
        private readonly char[,] _buffer = new char[height, width];

        public int Width => width;

        public int Height => height;

        public void Clear(PaletteColor color)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    _buffer[y, x] = ' ';
                }
            }
        }

        public void FillCircle(float centerX, float centerY, float radius, PaletteColor color)
        {
            var glyph = GlyphFor(color);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    // Characters are about twice as tall as they are wide.
                    var dx = (x - centerX) * 0.5f;
                    var dy = y - centerY;
                    if (MathF.Sqrt((dx * dx) + (dy * dy)) <= radius)
                    {
                        _buffer[y, x] = glyph;
                    }
                }
            }
        }

        public void FillRectangle(float x, float y, float w, float h, PaletteColor color)
        {
            var glyph = GlyphFor(color);
            for (var py = (int)y; py < y + h && py < height; py++)
            {
                for (var px = (int)x; px < x + w && px < width; px++)
                {
                    if (px >= 0 && py >= 0)
                    {
                        _buffer[py, px] = glyph;
                    }
                }
            }
        }

        public void DrawRectangle(float x, float y, float w, float h, PaletteColor color, float thickness = 1f)
        {
            var glyph = GlyphFor(color);
            for (var px = (int)x; px <= x + w && px < width; px++)
            {
                Plot(px, (int)y, glyph);
                Plot(px, (int)(y + h), glyph);
            }

            for (var py = (int)y; py <= y + h && py < height; py++)
            {
                Plot((int)x, py, glyph);
                Plot((int)(x + w), py, glyph);
            }
        }

        public void DrawLine(float x1, float y1, float x2, float y2, PaletteColor color, float thickness = 1f)
        {
            var steps = (int)MathF.Max(MathF.Abs(x2 - x1), MathF.Abs(y2 - y1));
            for (var i = 0; i <= steps; i++)
            {
                var t = steps == 0 ? 0f : i / (float)steps;
                Plot((int)(x1 + ((x2 - x1) * t)), (int)(y1 + ((y2 - y1) * t)), GlyphFor(color));
            }
        }

        public void DrawText(string text, float x, float y, PaletteColor color, float size = 12f)
        {
            for (var i = 0; i < text.Length; i++)
            {
                Plot((int)x + i, (int)y, text[i]);
            }
        }

        private void Plot(int x, int y, char glyph)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                _buffer[y, x] = glyph;
            }
        }

        private static char GlyphFor(PaletteColor color) => color switch
        {
            PaletteColor.Wall => '#',
            PaletteColor.Floor => '.',
            PaletteColor.Player1 => '@',
            PaletteColor.Player2 => '&',
            PaletteColor.Enemy => 'x',
            PaletteColor.Boss => 'X',
            PaletteColor.Projectile => '*',
            PaletteColor.Pickup => '$',
            _ => '?',
        };

        public override string ToString()
        {
            var builder = new StringBuilder();
            for (var y = 0; y < height; y++)
            {
                builder.Append("     ");
                for (var x = 0; x < width; x++)
                {
                    builder.Append(_buffer[y, x]);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }

    public void Run()
    {
        var surface = new ConsoleRenderSurface(68, 20);

        // Exactly the kind of calls WorldRenderer makes against the real GDI+ adapter.
        surface.Clear(PaletteColor.Background);
        surface.FillRectangle(1f, 1f, 65f, 17f, PaletteColor.Floor);
        surface.DrawRectangle(1f, 1f, 65f, 17f, PaletteColor.Wall);
        surface.FillCircle(20f, 10f, 2f, PaletteColor.Player1);
        surface.FillCircle(34f, 6f, 2f, PaletteColor.Player2);
        surface.FillCircle(50f, 12f, 2f, PaletteColor.Enemy);
        surface.FillCircle(58f, 5f, 3f, PaletteColor.Boss);
        surface.FillCircle(26f, 9f, 1f, PaletteColor.Projectile);
        surface.DrawText(" room demo-r1 ", 3f, 0f, PaletteColor.HudText);

        DemoConsole.Step("Target   : IRenderSurface (what Core draws through)");
        DemoConsole.Step("Adaptee  : a char[,] buffer here, System.Drawing.Graphics in the client");
        DemoConsole.Step("Adapter  : ConsoleRenderSurface here, GdiRenderSurface in the client");
        Console.WriteLine();
        Console.Write(surface.ToString());
        Console.WriteLine();
        DemoConsole.Step("Core drew this without referencing System.Drawing even once.");
    }
}
