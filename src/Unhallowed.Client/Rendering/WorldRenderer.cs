using Unhallowed.Contracts.Messages;
using Unhallowed.Core.Rendering;

namespace Unhallowed.Client.Rendering;

/// <summary>
/// Draws a snapshot. Note that it only ever talks to <see cref="IRenderSurface"/> - there is no
/// <c>System.Drawing</c> type anywhere in this file. Swapping GDI+ for another primitive backend
/// would mean writing one new adapter and changing nothing here.
/// </summary>
internal sealed class WorldRenderer
{
    private const float RoomWidth = 960f;
    private const float RoomHeight = 540f;

    public void Render(
        IRenderSurface surface,
        IReadOnlyList<EntitySnapshotDto> entities,
        int localPlayerId,
        string roomId,
        int tick,
        IReadOnlyList<string> log,
        bool connected)
    {
        surface.Clear(PaletteColor.Background);

        var scale = Math.Min(surface.Width / RoomWidth, (surface.Height - 60) / RoomHeight);
        var offsetX = (surface.Width - (RoomWidth * scale)) / 2f;
        var offsetY = 44f;

        DrawRoom(surface, scale, offsetX, offsetY);

        foreach (var entity in entities.Where(e => e.Kind != "player"))
        {
            DrawEntity(surface, entity, scale, offsetX, offsetY, localPlayerId);
        }

        // Players last so they are never hidden behind a tear or an enemy.
        foreach (var entity in entities.Where(e => e.Kind == "player"))
        {
            DrawEntity(surface, entity, scale, offsetX, offsetY, localPlayerId);
        }

        DrawHud(surface, entities, localPlayerId, roomId, tick, log, connected);
    }

    private static void DrawRoom(IRenderSurface surface, float scale, float offsetX, float offsetY)
    {
        surface.FillRectangle(offsetX, offsetY, RoomWidth * scale, RoomHeight * scale, PaletteColor.Floor);
        surface.DrawRectangle(offsetX, offsetY, RoomWidth * scale, RoomHeight * scale, PaletteColor.Wall, 3f);
    }

    private static void DrawEntity(
        IRenderSurface surface,
        EntitySnapshotDto entity,
        float scale,
        float offsetX,
        float offsetY,
        int localPlayerId)
    {
        var x = offsetX + (entity.X * scale);
        var y = offsetY + (entity.Y * scale);
        var radius = entity.Radius * scale;
        var color = Palette.ForVisual(entity.Visual);

        surface.FillCircle(x, y, radius, color);

        if (entity.Id == localPlayerId)
        {
            surface.DrawRectangle(
                x - radius - 4f, y - radius - 4f,
                (radius + 4f) * 2f, (radius + 4f) * 2f,
                PaletteColor.HudText, 1.5f);
        }

        if (entity.MaxHealth > 1 && entity.Kind != "projectile")
        {
            var barWidth = radius * 2f;
            var filled = barWidth * (entity.Health / (float)entity.MaxHealth);
            var barY = y - radius - 8f;

            surface.FillRectangle(x - radius, barY, barWidth, 3f, PaletteColor.HealthEmpty);
            surface.FillRectangle(x - radius, barY, filled, 3f, PaletteColor.HealthFull);
        }
    }

    private static void DrawHud(
        IRenderSurface surface,
        IReadOnlyList<EntitySnapshotDto> entities,
        int localPlayerId,
        string roomId,
        int tick,
        IReadOnlyList<string> log,
        bool connected)
    {
        var status = connected ? $"room {roomId}  tick {tick}" : "connecting...";
        surface.DrawText($"UNHALLOWED   {status}", 12f, 10f, PaletteColor.HudText, 11f);

        var players = entities.Where(e => e.Kind == "player").ToArray();
        surface.DrawText($"players {players.Length}/4", 12f, 26f, PaletteColor.HudText, 9f);

        var local = players.FirstOrDefault(p => p.Id == localPlayerId);
        if (local is not null)
        {
            surface.DrawText(
                $"HP {local.Health}/{local.MaxHealth}", 120f, 26f, PaletteColor.HealthFull, 9f);
        }

        surface.DrawText("WASD move | arrows shoot", surface.Width - 190f, 10f, PaletteColor.HudText, 9f);

        var logY = surface.Height - 14f - (log.Count * 13f);
        foreach (var line in log)
        {
            surface.DrawText(line, 12f, logY, PaletteColor.HudText, 8.5f);
            logY += 13f;
        }
    }
}
