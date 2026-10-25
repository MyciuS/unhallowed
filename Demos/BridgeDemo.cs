using Isaac.Game.Dungeon;
using Isaac.Game.Entities;
using Isaac.Net;
using Isaac.Rendering;
using Raylib_cs;
using Color = System.Drawing.Color;

namespace Isaac.Demos;

/// <summary>
/// BRIDGE pattern demo. Draws the same two HUD widgets (Refined Abstractions) on two canvases
/// (Concrete Implementors): first into an SVG file, then into a Raylib window. The widget classes
/// are identical in both cases; only the canvas passed to their constructor changes.
/// </summary>
public static class BridgeDemo
{
    private const string SvgFile = "bridge-demo.svg";
    private const int Pad = 20;
    private const int Width = BossBarWidget.Width + Pad * 2;
    private const int Height = Pad + MinimapWidget.Height + Pad + BossBarWidget.Height + Pad;

    private static readonly Color Background = Color.FromArgb(18, 14, 12);

    public static void Run()
    {
        Console.WriteLine("BRIDGE: HUD widgets (Abstraction) draw only through ICanvas (Implementor)");
        Console.WriteLine();
        Console.WriteLine("  Abstraction         HudWidget     -> MinimapWidget, BossBarWidget");
        Console.WriteLine("  Implementor         ICanvas       -> RaylibCanvas, SvgCanvas");
        Console.WriteLine("  N widgets and M canvases need N + M classes, not one subclass per pair (N x M)");

        var snapshot = SampleSnapshot();
        DrawToSvg(snapshot);
        DrawToWindow(snapshot);
    }

    private static HudWidget[] CreateWidgets(ICanvas canvas) =>
    [
        new MinimapWidget(canvas, Pad + BossBarWidget.Width - MinimapWidget.Width, Pad),
        new BossBarWidget(canvas, Pad, Pad + MinimapWidget.Height + Pad),
    ];

    private static void DrawToSvg(Snapshot snapshot)
    {
        var canvas = new SvgCanvas(Width, Height, Background);
        foreach (var widget in CreateWidgets(canvas))
            widget.Draw(snapshot);
        File.WriteAllText(SvgFile, canvas.ToSvg());

        Console.WriteLine();
        Console.WriteLine("== MinimapWidget + BossBarWidget on an SvgCanvas ==");
        Console.Write(canvas.ToSvg());
        Console.WriteLine($"  saved to {Path.GetFullPath(SvgFile)} (open it in a browser)");
    }

    private static void DrawToWindow(Snapshot snapshot)
    {
        Console.WriteLine();
        Console.WriteLine("== The same widgets on a RaylibCanvas ==");
        Console.WriteLine("  a window is open now; close it or press Esc to finish");

        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.InitWindow(Width, Height, "Bridge demo: RaylibCanvas");
        Raylib.SetTargetFPS(30);
        var widgets = CreateWidgets(new RaylibCanvas());
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Renderer.Background);
            foreach (var widget in widgets)
                widget.Draw(snapshot);
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }

    private static Snapshot SampleSnapshot()
    {
        var snapshot = new Snapshot();
        snapshot.Map.AddRange(
        [
            Room(-1, 0, RoomKind.Normal, MapCell.FlagVisited),
            Room(0, 0, RoomKind.Start, MapCell.FlagVisited),
            Room(0, 1, RoomKind.Treasure, MapCell.FlagVisited),
            Room(1, 0, RoomKind.Normal, MapCell.FlagVisited),
            Room(1, -1, RoomKind.Normal, 0),
            Room(2, 0, RoomKind.Boss, MapCell.FlagVisited | MapCell.FlagCurrent),
        ]);
        snapshot.Enemies.Add(new EnemyView { Kind = EnemyKind.Monstro, HpPercent = 65 });
        return snapshot;
    }

    private static MapCell Room(sbyte x, sbyte y, RoomKind kind, int flags) =>
        new() { X = x, Y = y, Kind = kind, Flags = (byte)flags };
}
