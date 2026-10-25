using System.Drawing;
using Isaac.Core;
using Isaac.Game.Dungeon;
using Isaac.Net;

namespace Isaac.Rendering;

/// <summary>BRIDGE pattern, a Refined Abstraction: the floor map, centred on the room the players are in.</summary>
public sealed class MinimapWidget : HudWidget
{
    public const int Width = 140;
    public const int Height = Config.HudHeight;

    private const int CellW = 16, CellH = 11, Gap = 2;

    private static readonly Color Current = Color.White;
    private static readonly Color Visited = Color.FromArgb(150, 150, 150);
    private static readonly Color Unvisited = Color.FromArgb(70, 70, 70);
    private static readonly Color BossIcon = Color.FromArgb(200, 40, 40);
    private static readonly Color TreasureIcon = Color.FromArgb(240, 200, 40);

    public MinimapWidget(ICanvas canvas, int x, int y)
        : base(canvas, x, y)
    {
    }

    public override void Draw(Snapshot snapshot)
    {
        if (snapshot.Map.Count == 0) return;
        var current = snapshot.Map.FirstOrDefault(m => (m.Flags & MapCell.FlagCurrent) != 0);
        int cx = X + Width / 2, cy = Y + Height / 2;
        foreach (var m in snapshot.Map)
        {
            int x = cx + (m.X - current.X) * (CellW + Gap) - CellW / 2;
            int y = cy + (m.Y - current.Y) * (CellH + Gap) - CellH / 2;
            if (x < X || x > X + Width - CellW || y < Y + 2 || y > Y + Height - CellH) continue;

            bool visited = (m.Flags & MapCell.FlagVisited) != 0;
            bool here = (m.Flags & MapCell.FlagCurrent) != 0;
            Canvas.FillRect(x, y, CellW, CellH, here ? Current : visited ? Visited : Unvisited);

            Color? icon = m.Kind switch
            {
                RoomKind.Boss => BossIcon,
                RoomKind.Treasure => TreasureIcon,
                _ => null,
            };
            if (icon is { } color) Canvas.FillRect(x + 5, y + 3, 6, 5, color);
        }
    }
}
