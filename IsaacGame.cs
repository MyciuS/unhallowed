using Isaac.Core;
using Isaac.Game;
using Isaac.Net;
using Isaac.Rendering;
using Raylib_cs;

namespace Isaac;

/// <summary>Owns the window and main loop. Shows the menu while no session is running, otherwise the game.</summary>
public sealed class IsaacGame
{
    private readonly Menu _menu;
    private IGameSession? _session; // null while the menu is shown
    private bool _quit;

    public IsaacGame() => _menu = new Menu(this);

    public void Quit() => _quit = true;

    public void StartSession(IGameSession session)
    {
        _session?.Dispose();
        _session = session;
    }

    public void ShowMenu(string? notice = null)
    {
        _session?.Dispose();
        _session = null;
        _menu.Notice = notice;
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(Config.ScreenW, Config.ScreenH, "Isaac Co-op");
        Raylib.SetExitKey(KeyboardKey.Null); // Esc is used for menus
        Raylib.SetTargetFPS(144);
        GameLogger.Instance.Log("Game started");

        while (!_quit && !Raylib.WindowShouldClose())
        {
            float dt = MathF.Min(Raylib.GetFrameTime(), 0.1f);
            if (_session == null) _menu.Update();
            else UpdatePlaying(_session, dt);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Renderer.Background);
            if (_session == null) _menu.Draw();
            else DrawPlaying(_session);
            Raylib.EndDrawing();
        }

        _session?.Dispose();
        Raylib.CloseWindow();
        GameLogger.Instance.Log("Game closed");
    }

    private void UpdatePlaying(IGameSession session, float dt)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            ShowMenu();
            return;
        }

        session.Update(dt, PlayerInput.ReadKeyboard());

        if (session.Error != null)
        {
            ShowMenu(session.Error);
            return;
        }

        if (session.IsHost && session.Latest?.GameOver == true && Raylib.IsKeyPressed(KeyboardKey.R))
            session.RequestRestart();
    }

    private static void DrawPlaying(IGameSession session)
    {
        var snapshot = session.Latest;
        if (snapshot == null)
        {
            Renderer.DrawCentered(session.Status, Config.ScreenH / 2 - 20, 28, Renderer.Text);
            Renderer.DrawCentered("Esc to cancel", Config.ScreenH / 2 + 20, 18, Renderer.TextDim);
            return;
        }

        Renderer.Draw(snapshot, session.LocalPlayerId);

        if (snapshot.GameOver)
        {
            string hint = session.IsHost ? "Press R to restart, Esc for menu" : "Waiting for host to restart...";
            Renderer.DrawCentered(hint, Config.ScreenH / 2 + 60, 20, Renderer.TextDim);
        }
        Raylib.DrawText(session.Status, 8, Config.ScreenH - 20, 12, new Color(255, 255, 255, 90));
    }
}
