using System.Drawing;
using System.Globalization;
using System.Net;
using System.Text;

namespace Isaac.Rendering;

/// <summary>
/// BRIDGE pattern, a Concrete Implementor: writes the drawing out as an SVG image instead of
/// showing it, so the HUD can be exported without a window or a graphics card.
/// </summary>
public sealed class SvgCanvas : ICanvas
{
    // Text is set in a monospace font, whose glyphs are 0.6 em wide.
    private const double GlyphWidth = 0.6;

    private readonly StringBuilder _shapes = new();

    public SvgCanvas(int width, int height, Color background)
    {
        Width = width;
        Height = height;
        FillRect(0, 0, width, height, background);
    }

    public int Width { get; }
    public int Height { get; }

    public void FillRect(int x, int y, int width, int height, Color color) =>
        _shapes.AppendLine($"  <rect x=\"{x}\" y=\"{y}\" width=\"{width}\" height=\"{height}\" {Fill(color)}/>");

    public void DrawText(string text, int x, int y, int size, Color color) =>
        _shapes.AppendLine(
            $"  <text x=\"{x}\" y=\"{y}\" font-size=\"{size}\" dominant-baseline=\"hanging\" {Fill(color)}>" +
            $"{WebUtility.HtmlEncode(text)}</text>");

    public int MeasureText(string text, int size) => (int)Math.Round(text.Length * size * GlyphWidth);

    public string ToSvg() =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Width}\" height=\"{Height}\" font-family=\"monospace\">\n" +
        $"{_shapes}</svg>\n";

    private static string Fill(Color c)
    {
        string fill = $"fill=\"#{c.R:x2}{c.G:x2}{c.B:x2}\"";
        return c.A == 255 ? fill : $"{fill} fill-opacity=\"{(c.A / 255.0).ToString("0.##", CultureInfo.InvariantCulture)}\"";
    }
}
