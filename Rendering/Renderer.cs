using System.Numerics;
using Isaac.Core;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Game.Items;
using Isaac.Net;
using Raylib_cs;

namespace Isaac.Rendering;

/// <summary>Draws a <see cref="Snapshot"/> with plain shapes (no textures needed).</summary>
public static class Renderer
{
    private const int Ox = Config.Wall;
    private const int Oy = Config.HudHeight + Config.Wall;

    public static readonly Color Background = new(18, 14, 12, 255);
    private static readonly Color WallColor = new(58, 44, 38, 255);
    private static readonly Color WallEdge = new(40, 30, 26, 255);
    private static readonly Color FloorA = new(112, 86, 66, 255);
    private static readonly Color FloorB = new(104, 80, 61, 255);
    private static readonly Color DoorDark = new(20, 15, 13, 255);
    private static readonly Color DoorFrame = new(90, 70, 58, 255);
    private static readonly Color Bars = new(70, 50, 30, 255);
    private static readonly Color RockColor = new(120, 120, 128, 255);
    private static readonly Color RockShade = new(84, 84, 92, 255);
    private static readonly Color HeartRed = new(214, 40, 40, 255);
    private static readonly Color HeartEmpty = new(60, 30, 30, 255);
    private static readonly Color TearBlue = new(150, 200, 255, 255);
    private static readonly Color TearRed = new(200, 40, 40, 255);
    public static readonly Color Text = new(235, 225, 210, 255);
    public static readonly Color TextDim = new(150, 140, 130, 255);

    public static readonly Color[] PlayerColors =
    [
        new(245, 205, 190, 255), // classic Isaac skin
        new(130, 200, 255, 255),
        new(170, 235, 120, 255),
        new(255, 185, 90, 255),
    ];

    private static readonly RaylibCanvas Canvas = new();
    private static readonly MinimapWidget Minimap = new(Canvas, Config.ScreenW - MinimapWidget.Width, 0);
    private static readonly BossBarWidget BossBar = new(Canvas, Config.ScreenW / 2 - BossBarWidget.Width / 2, Config.ScreenH - 60);

    private static Vector2 W(float x, float y) => new(Ox + x, Oy + y);

    public static void Draw(Snapshot s, int localPlayerId)
    {
        DrawRoom(s);
        foreach (var p in s.Pickups) DrawPickup(p);
        foreach (var e in s.Enemies) DrawEnemy(e);
        foreach (var p in s.Players) DrawPlayer(p, p.Id == localPlayerId);
        foreach (var t in s.Tears) DrawTear(t);
        DrawHud(s, localPlayerId);
        Minimap.Draw(s);
        BossBar.Draw(s);

        if (s.Message.Length > 0)
            DrawCentered(s.Message, Oy + 16, 26, Text, shadow: true);

        if (s.GameOver)
        {
            Raylib.DrawRectangle(0, 0, Config.ScreenW, Config.ScreenH, new Color(0, 0, 0, 170));
            DrawCentered("YOU DIED", Config.ScreenH / 2 - 60, 64, HeartRed, shadow: true);
            DrawCentered($"Floor {s.Floor}  -  Score {s.Score}", Config.ScreenH / 2 + 10, 24, Text);
        }
    }

    // ------------------------------------------------------------------ room

    private static void DrawRoom(Snapshot s)
    {
        Raylib.DrawRectangle(0, Config.HudHeight, Config.ScreenW, Config.ScreenH - Config.HudHeight, WallColor);
        Raylib.DrawRectangleLinesEx(new Rectangle(8, Config.HudHeight + 8, Config.ScreenW - 16, Config.ScreenH - Config.HudHeight - 16), 4, WallEdge);

        for (int x = 0; x < Config.RoomTilesW; x++)
        for (int y = 0; y < Config.RoomTilesH; y++)
            Raylib.DrawRectangle(Ox + x * Config.Tile, Oy + y * Config.Tile, Config.Tile, Config.Tile, (x + y) % 2 == 0 ? FloorA : FloorB);

        // Vignette-ish inner shadow along the walls.
        Raylib.DrawRectangleGradientV(Ox, Oy, Config.RoomW, 24, new Color(0, 0, 0, 90), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientH(Ox, Oy, 24, Config.RoomH, new Color(0, 0, 0, 70), new Color(0, 0, 0, 0));

        foreach (var d in Enum.GetValues<Direction>())
            if (s.HasDoor(d)) DrawDoor(d, s.DoorsOpen);

        foreach (byte idx in s.Rocks)
        {
            int tx = idx % Config.RoomTilesW, ty = idx / Config.RoomTilesW;
            var rect = new Rectangle(Ox + tx * Config.Tile + 4, Oy + ty * Config.Tile + 6, Config.Tile - 8, Config.Tile - 10);
            Raylib.DrawRectangleRounded(rect with { Y = rect.Y + 4 }, 0.4f, 6, RockShade);
            Raylib.DrawRectangleRounded(rect with { Height = rect.Height - 4 }, 0.4f, 6, RockColor);
        }
    }

    private static void DrawDoor(Direction d, bool open)
    {
        const int len = 80;
        var rect = d switch
        {
            Direction.Up => new Rectangle(Ox + Config.RoomW / 2f - len / 2f, Oy - Config.Wall + 8, len, Config.Wall - 8),
            Direction.Down => new Rectangle(Ox + Config.RoomW / 2f - len / 2f, Oy + Config.RoomH, len, Config.Wall - 8),
            Direction.Left => new Rectangle(Ox - Config.Wall + 8, Oy + Config.RoomH / 2f - len / 2f, Config.Wall - 8, len),
            _ => new Rectangle(Ox + Config.RoomW, Oy + Config.RoomH / 2f - len / 2f, Config.Wall - 8, len),
        };
        Raylib.DrawRectangleRec(new Rectangle(rect.X - 6, rect.Y - 6, rect.Width + 12, rect.Height + 12), DoorFrame);
        Raylib.DrawRectangleRec(rect, DoorDark);
        if (open) return;

        bool vertical = d is Direction.Up or Direction.Down;
        for (int i = 1; i <= 3; i++)
        {
            if (vertical)
                Raylib.DrawRectangle((int)(rect.X + rect.Width * i / 4f) - 4, (int)rect.Y, 8, (int)rect.Height, Bars);
            else
                Raylib.DrawRectangle((int)rect.X, (int)(rect.Y + rect.Height * i / 4f) - 4, (int)rect.Width, 8, Bars);
        }
    }

    // -------------------------------------------------------------- entities

    private static void DrawPlayer(PlayerView p, bool local)
    {
        var pos = W(p.X, p.Y);
        var color = PlayerColors[p.Id % PlayerColors.Length];
        bool dead = p.Health == 0;
        if (dead) color = color with { A = 80 };
        else if (p.Blink && (int)(Raylib.GetTime() * 12) % 2 == 0) return;

        Raylib.DrawEllipse((int)pos.X, (int)pos.Y + 18, 16, 6, new Color(0, 0, 0, 60));
        // Body + head.
        Raylib.DrawCircleV(pos + new Vector2(0, 10), 11, color);
        Raylib.DrawCircleV(pos - new Vector2(0, 4), 17, color);
        // Eyes look where the player is shooting/moving.
        var look = new Vector2(p.LookX, p.LookY) * 4;
        var eye = dead ? new Color(0, 0, 0, 80) : new Color(30, 20, 20, 255);
        if (p.LookY >= 0 || p.LookX != 0)
        {
            Raylib.DrawCircleV(pos + new Vector2(-6, -5) + look, 4, eye);
            Raylib.DrawCircleV(pos + new Vector2(6, -5) + look, 4, eye);
        }

        string label = $"P{p.Id + 1} {p.Character}";
        int w = Raylib.MeasureText(label, 14);
        Raylib.DrawText(label, (int)pos.X - w / 2, (int)pos.Y - 40, 14, local ? Color.White : TextDim);
    }

    private static void DrawEnemy(EnemyView e)
    {
        var pos = W(e.X, e.Y);
        float r = e.Radius;
        Color body = e.Kind switch
        {
            EnemyKind.Fly => new Color(50, 50, 55, 255),
            EnemyKind.Gaper => new Color(220, 170, 160, 255),
            EnemyKind.Spitter => new Color(120, 170, 80, 255),
            EnemyKind.Charger => new Color(200, 190, 150, 255),
            _ => new Color(200, 90, 90, 255),
        };
        if (e.Flash) body = Color.White;

        Raylib.DrawEllipse((int)pos.X, (int)(pos.Y + r * 0.9f), r, r * 0.3f, new Color(0, 0, 0, 60));
        switch (e.Kind)
        {
            case EnemyKind.Fly:
                float flap = MathF.Sin((float)Raylib.GetTime() * 40) * 3;
                Raylib.DrawCircleV(pos + new Vector2(-9, -8 + flap), 7, new Color(220, 220, 230, 160));
                Raylib.DrawCircleV(pos + new Vector2(9, -8 - flap), 7, new Color(220, 220, 230, 160));
                Raylib.DrawCircleV(pos, r, body);
                Raylib.DrawCircleV(pos + new Vector2(-4, -2), 2.5f, new Color(200, 30, 30, 255));
                Raylib.DrawCircleV(pos + new Vector2(4, -2), 2.5f, new Color(200, 30, 30, 255));
                break;
            case EnemyKind.Charger:
                // Segmented maggot.
                Raylib.DrawCircleV(pos + new Vector2(-r * 0.6f, 2), r * 0.7f, body);
                Raylib.DrawCircleV(pos + new Vector2(r * 0.6f, 2), r * 0.7f, body);
                Raylib.DrawCircleV(pos, r * 0.85f, body);
                Raylib.DrawCircleV(pos + new Vector2(-4, -3), 3, Color.Black);
                Raylib.DrawCircleV(pos + new Vector2(4, -3), 3, Color.Black);
                break;
            default:
                Raylib.DrawCircleV(pos, r, body);
                float eyeR = r * 0.18f;
                Raylib.DrawCircleV(pos + new Vector2(-r * 0.35f, -r * 0.2f), eyeR, Color.Black);
                Raylib.DrawCircleV(pos + new Vector2(r * 0.35f, -r * 0.2f), eyeR, Color.Black);
                Raylib.DrawEllipse((int)pos.X, (int)(pos.Y + r * 0.35f), r * 0.4f, r * 0.2f, new Color(60, 10, 10, 255));
                break;
        }
    }

    private static void DrawTear(TearView t)
    {
        var pos = W(t.X, t.Y);
        var color = t.FromEnemy ? TearRed : TearBlue;
        Raylib.DrawCircleV(pos + new Vector2(0, t.Radius + 6), t.Radius * 0.6f, new Color(0, 0, 0, 50));
        Raylib.DrawCircleV(pos, t.Radius, color);
        Raylib.DrawCircleV(pos - new Vector2(t.Radius * 0.3f), t.Radius * 0.35f, new Color(255, 255, 255, 150));
    }

    private static void DrawPickup(PickupView p)
    {
        var pos = W(p.X, p.Y);
        switch (p.Kind)
        {
            case PickupKind.Heart:
                DrawHeart(pos, 10, 2);
                break;
            case PickupKind.Trapdoor:
                Raylib.DrawRectangleRec(new Rectangle(pos.X - 32, pos.Y - 32, 64, 64), new Color(60, 45, 35, 255));
                Raylib.DrawRectangleRec(new Rectangle(pos.X - 26, pos.Y - 26, 52, 52), Color.Black);
                break;
            case PickupKind.Item:
                float bob = MathF.Sin((float)Raylib.GetTime() * 3) * 4;
                Raylib.DrawRectangleRec(new Rectangle(pos.X - 22, pos.Y + 4, 44, 24), new Color(150, 150, 160, 255));
                Raylib.DrawRectangleRec(new Rectangle(pos.X - 26, pos.Y + 2, 52, 8), new Color(175, 175, 185, 255));
                Raylib.DrawCircleV(pos + new Vector2(0, -14 + bob), 18, new Color(255, 240, 160, 60));
                Raylib.DrawCircleV(pos + new Vector2(0, -14 + bob), 12, ItemColor(p.Item));
                string name = ItemCatalog.Name(p.Item);
                int w = Raylib.MeasureText(name, 14);
                Raylib.DrawText(name, (int)pos.X - w / 2, (int)(pos.Y + 34), 14, Text);
                break;
        }
    }

    private static Color ItemColor(ItemKind item) => item switch
    {
        ItemKind.DamageUp => new Color(230, 60, 60, 255),
        ItemKind.TearsUp => new Color(80, 160, 255, 255),
        ItemKind.SpeedUp => new Color(250, 220, 70, 255),
        ItemKind.RangeUp => new Color(120, 230, 140, 255),
        ItemKind.HeartContainer => HeartRed,
        _ => new Color(200, 230, 255, 255),
    };

    /// <summary>Heart shape; fill: 0 empty, 1 half, 2 full.</summary>
    private static void DrawHeart(Vector2 c, float s, int fill)
    {
        DrawHeartShape(c, s, HeartEmpty);
        if (fill == 0) return;
        if (fill == 1) Raylib.BeginScissorMode((int)(c.X - s * 1.2f), (int)(c.Y - s * 2), (int)(s * 1.2f), (int)(s * 4));
        DrawHeartShape(c, s, HeartRed);
        if (fill == 1) Raylib.EndScissorMode();
    }

    private static void DrawHeartShape(Vector2 c, float s, Color color)
    {
        Raylib.DrawCircleV(c + new Vector2(-s * 0.5f, -s * 0.25f), s * 0.55f, color);
        Raylib.DrawCircleV(c + new Vector2(s * 0.5f, -s * 0.25f), s * 0.55f, color);
        Raylib.DrawTriangle(c + new Vector2(-s * 1.03f, -s * 0.05f), c + new Vector2(0, s), c + new Vector2(s * 1.03f, -s * 0.05f), color);
    }

    // ------------------------------------------------------------------- HUD

    private static void DrawHud(Snapshot s, int localId)
    {
        Raylib.DrawRectangle(0, 0, Config.ScreenW, Config.HudHeight, Background);
        int x = 14;
        foreach (var p in s.Players.OrderBy(p => p.Id))
        {
            var color = PlayerColors[p.Id % PlayerColors.Length];
            Raylib.DrawText(p.Id == localId ? $"P{p.Id + 1} (you)" : $"P{p.Id + 1}", x, 8, 16, color);
            for (int h = 0; h < (p.MaxHealth + 1) / 2; h++)
            {
                int fill = Math.Clamp(p.Health - h * 2, 0, 2);
                DrawHeart(new Vector2(x + 9 + h % 8 * 18, 33 + h / 8 * 15), 7, fill);
            }
            Raylib.DrawText($"DMG {p.Damage:0.#}  TRS {p.TearsPerSecond:0.#}  SPD {p.Speed / 100:0.#}", x, 58, 10, TextDim);
            x += 165;
        }

        // Floor and score live in the top wall, out of the way of the player columns.
        Raylib.DrawText($"Floor {s.Floor}   Score {s.Score}", 20, Config.HudHeight + 22, 18, Text);
    }

    public static void DrawCentered(string text, int y, int size, Color color, bool shadow = false)
    {
        int w = Raylib.MeasureText(text, size);
        int x = Config.ScreenW / 2 - w / 2;
        if (shadow) Raylib.DrawText(text, x + 2, y + 2, size, Color.Black);
        Raylib.DrawText(text, x, y, size, color);
    }
}
