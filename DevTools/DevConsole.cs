using System.Numerics;
using Isaac.Core;
using Isaac.Game;
using Isaac.Game.Entities;
using Isaac.Game.Items;
using Isaac.Net;
using Isaac.Rendering;
using Raylib_cs;

namespace Isaac.DevTools;

/// <summary>
/// In-game developer console, opened with F6. The Cheats tab changes the <see cref="World"/>
/// directly, so it only works where the World lives: in singleplayer and for the host of a
/// co-op game. The Patterns tab lists the course's required patterns and runs their demos.
/// </summary>
public sealed class DevConsole
{
    private enum Tab { Cheats, Patterns }

    private const int Left = 50;
    private const int Top = 92;
    private const int ButtonW = 250, ButtonH = 34, Gap = 8;
    private const int OutputSize = 12, OutputLine = 14;

    private static readonly Rectangle Panel = new(30, 30, Config.ScreenW - 60, Config.ScreenH - 60);
    private static readonly Color PanelColor = new(14, 11, 10, 240);
    private static readonly Color Border = new(90, 70, 58, 255);
    private static readonly Color ButtonColor = new(52, 40, 34, 255);
    private static readonly Color ButtonHover = new(84, 64, 52, 255);
    private static readonly Color ButtonOn = new(56, 120, 56, 255);
    private static readonly Color Good = new(120, 210, 120, 255);
    private static readonly Color Bad = new(214, 96, 80, 255);
    private static readonly Color Warning = new(250, 220, 70, 255);

    private static readonly (string Label, ItemKind Item)[] ItemCheats =
    [
        ("Damage Up", ItemKind.DamageUp),
        ("Tears Up", ItemKind.TearsUp),
        ("Speed Up", ItemKind.SpeedUp),
        ("Range Up", ItemKind.RangeUp),
        ("HP Up (+1 heart)", ItemKind.HeartContainer),
    ];

    private readonly DemoRunner _demos = new();
    private Tab _tab = Tab.Cheats;
    private int _selected;
    private int _scroll;
    private string _status = "";
    private Font _mono;
    private bool _monoLoaded;

    public bool IsOpen { get; private set; }

    /// <summary>
    /// Handles the keys that open and close the console. Call before the game handles Esc. Movement
    /// and shooting keep going to the game while the console is open: a host has to keep simulating
    /// for the players connected to it.
    /// </summary>
    public void Update()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F6))
            IsOpen = !IsOpen;
        else if (IsOpen && Raylib.IsKeyPressed(KeyboardKey.Escape))
            IsOpen = false;

        _demos.Poll();
        if (IsOpen && _tab == Tab.Patterns)
            _scroll = Math.Max(0, _scroll - (int)(Raylib.GetMouseWheelMove() * 3));
    }

    /// <summary>Draws the console on top of everything and runs whatever was clicked.</summary>
    public void Draw(IGameSession? session)
    {
        var world = (session as HostSession)?.World;
        if (!IsOpen)
        {
            if (world != null) DrawActiveCheats(world);
            return;
        }

        Raylib.DrawRectangleRec(Panel, PanelColor);
        Raylib.DrawRectangleLinesEx(Panel, 2, Border);
        Raylib.DrawText("DEV CONSOLE", Left, 44, 22, Renderer.Text);
        Raylib.DrawText("F6 or Esc to close", Left + 180, 50, 14, Renderer.TextDim);
        if (Button(new Rectangle(Panel.X + Panel.Width - 290, 40, 130, 32), "Cheats", _tab == Tab.Cheats)) _tab = Tab.Cheats;
        if (Button(new Rectangle(Panel.X + Panel.Width - 150, 40, 130, 32), "Patterns", _tab == Tab.Patterns)) _tab = Tab.Patterns;

        if (_tab == Tab.Cheats) DrawCheats(session, world);
        else DrawPatterns();
    }

    // ---------------------------------------------------------------- cheats

    private void DrawCheats(IGameSession? session, World? world)
    {
        if (session == null || world == null)
        {
            string why = session == null
                ? "Start a game first: Singleplayer or Host Co-op."
                : "Cheats only work for the host. The host runs the only real copy of the game; " +
                  "a joined player can send nothing but movement and shooting, so nobody can cheat from a client.";
            DrawWrapped(why, Left, Top, (int)Panel.Width - 40, 18, Renderer.Text);
            return;
        }

        world.Players.TryGetValue(session.LocalPlayerId, out var me);
        DrawPlayerColumn(world, me);
        DrawWorldColumn(session, world);
        DrawToggleColumn(world);

        int y = Top + 28 + 6 * (ButtonH + Gap) + 10;
        if (me != null)
            Raylib.DrawText(
                $"P{me.Id + 1} {me.Character.Name}   HP {me.Health}/{me.Stats.MaxHealth}   DMG {me.Stats.Damage:0.#}" +
                $"   TEARS/S {1 / me.Stats.FireDelay:0.#}   SPEED {me.Stats.Speed:0}   ITEMS {me.Items.Count}",
                Left, y, 18, Renderer.Text);
        Raylib.DrawText($"Floor {world.Floor.Number}   Room {world.Room.Kind}   Enemies {world.Enemies.Count}   Score {world.Score}",
            Left, y + 28, 18, Renderer.TextDim);
        Raylib.DrawText(_status, Left, y + 64, 18, Good);
    }

    private void DrawPlayerColumn(World world, Player? me)
    {
        int x = Left;
        Raylib.DrawText("YOUR PLAYER", x, Top, 16, Renderer.TextDim);
        int y = Top + 28;
        foreach (var (label, item) in ItemCheats)
        {
            if (Button(new Rectangle(x, y, ButtonW, ButtonH), label, enabled: me is { Alive: true }))
            {
                world.GiveItem(me!, item);
                _status = $"Gave {label}";
            }
            y += ButtonH + Gap;
        }
        if (Button(new Rectangle(x, y, ButtonW, ButtonH), "Full heal / revive", enabled: me != null))
        {
            world.Heal(me!);
            _status = "Healed";
        }
    }

    private void DrawWorldColumn(IGameSession session, World world)
    {
        int x = Left + ButtonW + 40;
        Raylib.DrawText("ROOM AND FLOOR", x, Top, 16, Renderer.TextDim);
        int y = Top + 28;
        if (Button(new Rectangle(x, y, ButtonW, ButtonH), "Kill all enemies", enabled: world.Enemies.Count > 0))
        {
            _status = world.Enemies.Count == 1 ? "Killed 1 enemy" : $"Killed {world.Enemies.Count} enemies";
            world.KillAllEnemies();
        }
        y += ButtonH + Gap;
        if (Button(new Rectangle(x, y, ButtonW, ButtonH), "Go to boss room"))
            _status = world.EnterBossRoom() ? "Entered the boss room" : "No other boss room left to fight on this floor";
        y += ButtonH + Gap;
        if (Button(new Rectangle(x, y, ButtonW, ButtonH), "Next floor"))
        {
            world.SkipToNextFloor();
            _status = $"Skipped to floor {world.Floor.Number}";
        }
        y += ButtonH + Gap;
        if (Button(new Rectangle(x, y, ButtonW, ButtonH), "Reveal map"))
        {
            world.RevealMap();
            _status = "Map revealed";
        }
        y += ButtonH + Gap;
        if (Button(new Rectangle(x, y, ButtonW, ButtonH), "Restart run"))
        {
            session.RequestRestart();
            _status = "Run restarted";
        }
    }

    private void DrawToggleColumn(World world)
    {
        int x = Left + (ButtonW + 40) * 2;
        Raylib.DrawText("TOGGLES", x, Top, 16, Renderer.TextDim);
        int y = Top + 28;
        if (Button(new Rectangle(x, y, ButtonW, ButtonH), $"God mode: {OnOff(world.GodMode)}", world.GodMode))
            world.GodMode = !world.GodMode;
        y += ButtonH + Gap;
        if (Button(new Rectangle(x, y, ButtonW, ButtonH), $"One-shot kills: {OnOff(world.OneShotKills)}", world.OneShotKills))
            world.OneShotKills = !world.OneShotKills;
    }

    private static string OnOff(bool on) => on ? "ON" : "OFF";

    private static void DrawActiveCheats(World world)
    {
        string text = (world.GodMode ? "GOD MODE  " : "") + (world.OneShotKills ? "ONE-SHOT KILLS  " : "");
        if (text.Length > 0)
            Raylib.DrawText(text + "(F6)", 8, Config.ScreenH - 38, 14, Warning);
    }

    // -------------------------------------------------------------- patterns

    private void DrawPatterns()
    {
        var patterns = PatternCatalog.All;
        int y = Top;
        for (int i = 0; i < patterns.Count; i++)
        {
            var p = patterns[i];
            if (Button(new Rectangle(Left, y, ButtonW, ButtonH), "", i == _selected))
            {
                _selected = i;
                _scroll = 0;
            }
            Raylib.DrawText($"{p.Letter}  {p.Name}", Left + 10, y + 9, 16, Renderer.Text);
            bool merged = PatternCatalog.IsInBuild(p);
            string tag = merged ? "in build" : "missing";
            Raylib.DrawText(tag, Left + ButtonW - 10 - Raylib.MeasureText(tag, 14), y + 10, 14, merged ? Good : Bad);
            y += ButtonH + 4;
        }
        DrawPatternDetails(patterns[_selected]);
    }

    private void DrawPatternDetails(PatternInfo pattern)
    {
        int x = Left + ButtonW + 30;
        int width = (int)(Panel.X + Panel.Width) - 20 - x;
        bool merged = PatternCatalog.IsInBuild(pattern);
        string? demo = PatternCatalog.DemoFor(pattern);

        Raylib.DrawText(pattern.Name, x, Top, 26, Renderer.Text);
        int y = DrawWrapped($"Requirement 10.{pattern.Letter}: {pattern.Requirement}", x, Top + 36, width, 16, Renderer.Text);
        Raylib.DrawText($"Code: {pattern.Where}", x, y + 6, 16, Renderer.TextDim);
        Raylib.DrawText(merged ? "In this build" : "Not in this build yet", x, y + 30, 16, merged ? Good : Bad);
        Raylib.DrawText(demo != null ? $"Demo: dotnet run -- --demo {demo}" : "No --demo for it yet", x, y + 54, 16, Renderer.TextDim);

        var run = new Rectangle(x + width - 150, y + 30, 150, ButtonH);
        if (Button(run, _demos.Running != null ? "Running..." : "Run demo", enabled: demo != null && _demos.Running == null))
        {
            _demos.Start(demo!);
            _scroll = 0;
        }

        DrawDemoOutput(new Rectangle(x, y + 84, width, Panel.Y + Panel.Height - 20 - (y + 84)));
    }

    private void DrawDemoOutput(Rectangle box)
    {
        Raylib.DrawRectangleRec(box, Color.Black);
        Raylib.DrawRectangleLinesEx(box, 1, Border);
        IReadOnlyList<string> lines = _demos.Running != null ? [$"Running --demo {_demos.Running} ..."]
            : _demos.Finished != null ? _demos.Lines
            : ["Pick a pattern and press Run demo. The output is the same as in the terminal."];

        int visible = (int)(box.Height - 12) / OutputLine;
        _scroll = Math.Min(_scroll, Math.Max(0, lines.Count - visible));

        Raylib.BeginScissorMode((int)box.X + 1, (int)box.Y + 1, (int)box.Width - 2, (int)box.Height - 2);
        for (int i = 0; i < visible && _scroll + i < lines.Count; i++)
            Raylib.DrawTextEx(Mono, lines[_scroll + i], new Vector2(box.X + 8, box.Y + 6 + i * OutputLine), OutputSize, 0, Renderer.Text);
        Raylib.EndScissorMode();

        if (lines.Count > visible)
            Raylib.DrawText($"{_scroll + 1}-{Math.Min(lines.Count, _scroll + visible)} of {lines.Count}  (mouse wheel)",
                (int)box.X, (int)(box.Y + box.Height + 2), 12, Renderer.TextDim);
    }

    // The demos print tables, which only line up in a monospaced font.
    private Font Mono
    {
        get
        {
            if (_monoLoaded) return _mono;
            string consolas = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "consola.ttf");
            if (File.Exists(consolas))
            {
                _mono = Raylib.LoadFontEx(consolas, OutputSize * 2, null!, 0);
                Raylib.SetTextureFilter(_mono.Texture, TextureFilter.Bilinear);
            }
            else
            {
                // Raylib's default font is shared with all other text, so its filter is left alone.
                _mono = Raylib.GetFontDefault();
            }
            _monoLoaded = true;
            return _mono;
        }
    }

    // ---------------------------------------------------------------- widgets

    private static bool Button(Rectangle r, string label, bool active = false, bool enabled = true)
    {
        bool hover = enabled && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), r);
        Raylib.DrawRectangleRec(r, active ? ButtonOn : hover ? ButtonHover : ButtonColor);
        Raylib.DrawRectangleLinesEx(r, 1, Border);
        if (label.Length > 0)
        {
            int w = Raylib.MeasureText(label, 16);
            Raylib.DrawText(label, (int)(r.X + (r.Width - w) / 2), (int)(r.Y + (r.Height - 16) / 2), 16,
                enabled ? Renderer.Text : Renderer.TextDim);
        }
        return hover && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    /// <returns>The y just below the last line.</returns>
    private static int DrawWrapped(string text, int x, int y, int width, int size, Color color)
    {
        string line = "";
        foreach (string word in text.Split(' '))
        {
            string next = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && Raylib.MeasureText(next, size) > width)
            {
                Raylib.DrawText(line, x, y, size, color);
                y += size + 6;
                line = word;
            }
            else
            {
                line = next;
            }
        }
        Raylib.DrawText(line, x, y, size, color);
        return y + size + 6;
    }
}
