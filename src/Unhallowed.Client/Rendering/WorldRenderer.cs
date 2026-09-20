using System.Drawing;
using System.Drawing.Drawing2D;
using Unhallowed.Contracts;
using Unhallowed.Contracts.Messages;

namespace Unhallowed.Client.Rendering;

/// <summary>
/// Draws a world snapshot with primitive GDI+ calls onto a double-buffered surface -
/// no engine, no sprite pipeline. It only ever reads the snapshot; it never simulates.
/// </summary>
internal sealed class WorldRenderer
{
    private static readonly Color Background = Color.FromArgb(18, 16, 22);
    private static readonly Color Floor = Color.FromArgb(34, 30, 40);
    private static readonly Color Wall = Color.FromArgb(52, 46, 60);
    private static readonly Color HudText = Color.FromArgb(220, 216, 210);

    private static readonly Color[] SeatColors =
    [
        Color.FromArgb(236, 226, 210),
        Color.FromArgb(120, 190, 255),
        Color.FromArgb(150, 235, 160),
        Color.FromArgb(255, 190, 120),
    ];

    public void Render(
        Graphics graphics,
        Size clientSize,
        IReadOnlyList<PlayerSnapshotDto> players,
        int localPlayerId,
        int tick,
        IReadOnlyList<string> log,
        bool connected)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Background);

        var scale = Math.Min(clientSize.Width / Protocol.ArenaWidth, (clientSize.Height - 60) / Protocol.ArenaHeight);
        var offsetX = (clientSize.Width - (Protocol.ArenaWidth * scale)) / 2f;
        const float offsetY = 44f;

        DrawArena(graphics, scale, offsetX, offsetY);

        foreach (var player in players)
        {
            DrawPlayer(graphics, player, scale, offsetX, offsetY, localPlayerId);
        }

        DrawHud(graphics, players, localPlayerId, tick, log, connected);
    }

    private static void DrawArena(Graphics graphics, float scale, float offsetX, float offsetY)
    {
        var width = Protocol.ArenaWidth * scale;
        var height = Protocol.ArenaHeight * scale;

        using var floor = new SolidBrush(Floor);
        using var wall = new Pen(Wall, 3f);
        graphics.FillRectangle(floor, offsetX, offsetY, width, height);
        graphics.DrawRectangle(wall, offsetX, offsetY, width, height);
    }

    private static void DrawPlayer(
        Graphics graphics,
        PlayerSnapshotDto player,
        float scale,
        float offsetX,
        float offsetY,
        int localPlayerId)
    {
        var x = offsetX + (player.X * scale);
        var y = offsetY + (player.Y * scale);
        var radius = Protocol.PlayerRadius * scale;
        var color = SeatColors[player.Seat % SeatColors.Length];

        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, x - radius, y - radius, radius * 2f, radius * 2f);

        if (player.Id == localPlayerId)
        {
            using var highlight = new Pen(HudText, 1.5f);
            graphics.DrawRectangle(highlight, x - radius - 4f, y - radius - 4f, (radius + 4f) * 2f, (radius + 4f) * 2f);
        }

        using var textBrush = new SolidBrush(HudText);
        using var font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold);
        var label = player.DisplayName;
        var size = graphics.MeasureString(label, font);
        graphics.DrawString(label, font, textBrush, x - (size.Width / 2f), y - radius - 16f);
    }

    private static void DrawHud(
        Graphics graphics,
        IReadOnlyList<PlayerSnapshotDto> players,
        int localPlayerId,
        int tick,
        IReadOnlyList<string> log,
        bool connected)
    {
        using var textBrush = new SolidBrush(HudText);
        using var title = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Bold);
        using var small = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold);

        var status = connected ? $"tick {tick}" : "connecting...";
        graphics.DrawString($"UNHALLOWED   {status}", title, textBrush, 12f, 10f);
        graphics.DrawString($"players {players.Count}/{Protocol.MaxPlayers}", small, textBrush, 12f, 28f);
        graphics.DrawString("WASD / arrows to move", small, textBrush, graphics.VisibleClipBounds.Width - 190f, 12f);

        var logY = graphics.VisibleClipBounds.Height - 14f - (log.Count * 13f);
        foreach (var line in log)
        {
            graphics.DrawString(line, small, textBrush, 12f, logY);
            logY += 13f;
        }
    }
}
