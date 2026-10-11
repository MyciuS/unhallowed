using Isaac.Core;
using Isaac.Game;
using Isaac.Net;
using Isaac.Rendering;
using Raylib_cs;

namespace Isaac.States;

/// <summary>The in-game state. Works the same whether offline, hosting or joined.</summary>
public sealed class PlayingState(IsaacGame game, IGameSession session) : IGameState
{
    private readonly InputHandler _input = new();

    public void Enter() { }

    public void Exit() => session.Dispose();

    public void Update(float dt)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            game.States.Change(new MenuState(game));
            return;
        }

        session.Update(dt, _input.Poll());

        if (session.Error != null)
        {
            game.States.Change(new MenuState(game, session.Error));
            return;
        }

        if (session.IsHost && session.Latest?.GameOver == true && Raylib.IsKeyPressed(KeyboardKey.R))
            session.RequestRestart();
    }

    public void Draw()
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
