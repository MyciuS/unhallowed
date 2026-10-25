using System.Drawing;
using Isaac.Game.Entities;
using Isaac.Net;

namespace Isaac.Rendering;

/// <summary>BRIDGE pattern, a Refined Abstraction: Monstro's health bar with his name above it.</summary>
public sealed class BossBarWidget : HudWidget
{
    public const int Width = 360;
    public const int Height = TitleHeight + BarHeight + 2;

    private const int TitleHeight = 20, BarHeight = 14;

    private static readonly Color Frame = Color.Black;
    private static readonly Color Health = Color.FromArgb(214, 40, 40);
    private static readonly Color Title = Color.FromArgb(235, 225, 210);

    public BossBarWidget(ICanvas canvas, int x, int y)
        : base(canvas, x, y)
    {
    }

    public override void Draw(Snapshot snapshot)
    {
        foreach (var e in snapshot.Enemies)
        {
            if (e.Kind != EnemyKind.Monstro) continue;
            int barY = Y + TitleHeight;
            Canvas.FillRect(X - 2, barY - 2, Width + 4, BarHeight + 4, Frame);
            Canvas.FillRect(X, barY, Width * e.HpPercent / 100, BarHeight, Health);
            DrawCenteredText("MONSTRO", X + Width / 2, Y, 16, Title, shadow: true);
            return;
        }
    }
}
