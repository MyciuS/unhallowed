using System.Drawing;
using System.Drawing.Drawing2D;
using Unhallowed.Core.Rendering;

namespace Unhallowed.Client.Rendering;

// PATTERN: Adapter -> docs/patterns/06-adapter.md
//   Target   : IRenderSurface          (what Core wants to talk to)
//   Adaptee  : System.Drawing.Graphics (what WinForms actually gives us - requirement 8)
//   Adapter  : this class
// The adaptee's API is incompatible with the target: Graphics wants Brush/Pen/RectangleF objects
// and centre-less rectangles, Core wants primitives and abstract colours. This class translates.

/// <summary>Object adapter over GDI+ so the domain can draw without referencing System.Drawing.</summary>
internal sealed class GdiRenderSurface : IRenderSurface, IDisposable
{
    private readonly Graphics _graphics;
    private readonly Dictionary<PaletteColor, SolidBrush> _brushes = [];
    private readonly Dictionary<(PaletteColor Color, float Width), Pen> _pens = [];
    private readonly Dictionary<float, Font> _fonts = [];

    public GdiRenderSurface(Graphics graphics, int width, int height)
    {
        _graphics = graphics;
        Width = width;
        Height = height;

        _graphics.SmoothingMode = SmoothingMode.AntiAlias;
        _graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        _graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
    }

    public int Width { get; }

    public int Height { get; }

    public void Clear(PaletteColor color) => _graphics.Clear(Palette.ToGdi(color));

    public void FillCircle(float centerX, float centerY, float radius, PaletteColor color)
        => _graphics.FillEllipse(
            BrushFor(color), centerX - radius, centerY - radius, radius * 2f, radius * 2f);

    public void FillRectangle(float x, float y, float width, float height, PaletteColor color)
        => _graphics.FillRectangle(BrushFor(color), x, y, width, height);

    public void DrawRectangle(float x, float y, float width, float height, PaletteColor color, float thickness = 1f)
        => _graphics.DrawRectangle(PenFor(color, thickness), x, y, width, height);

    public void DrawLine(float x1, float y1, float x2, float y2, PaletteColor color, float thickness = 1f)
        => _graphics.DrawLine(PenFor(color, thickness), x1, y1, x2, y2);

    public void DrawText(string text, float x, float y, PaletteColor color, float size = 12f)
        => _graphics.DrawString(text, FontFor(size), BrushFor(color), x, y);

    private SolidBrush BrushFor(PaletteColor color)
    {
        if (!_brushes.TryGetValue(color, out var brush))
        {
            brush = new SolidBrush(Palette.ToGdi(color));
            _brushes[color] = brush;
        }

        return brush;
    }

    private Pen PenFor(PaletteColor color, float width)
    {
        var key = (color, width);
        if (!_pens.TryGetValue(key, out var pen))
        {
            pen = new Pen(Palette.ToGdi(color), width);
            _pens[key] = pen;
        }

        return pen;
    }

    private Font FontFor(float size)
    {
        if (!_fonts.TryGetValue(size, out var font))
        {
            font = new Font(FontFamily.GenericSansSerif, size, FontStyle.Bold);
            _fonts[size] = font;
        }

        return font;
    }

    public void Dispose()
    {
        foreach (var brush in _brushes.Values)
        {
            brush.Dispose();
        }

        foreach (var pen in _pens.Values)
        {
            pen.Dispose();
        }

        foreach (var font in _fonts.Values)
        {
            font.Dispose();
        }
    }
}
