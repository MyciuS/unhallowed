using Isaac.Core;
using Isaac.Net;
using Isaac.Rendering;
using Raylib_cs;

namespace Isaac;

/// <summary>Main menu: pick singleplayer, host, join or quit.</summary>
public sealed class Menu(IsaacGame game)
{
    private static readonly string[] Options = ["Singleplayer", "Host Co-op", "Join Co-op", "Quit"];

    private string _address = "127.0.0.1";
    private int _selected;
    private bool _editingAddress;

    /// <summary>Shown under the menu, e.g. why the last connection ended.</summary>
    public string? Notice { get; set; }

    public void Update()
    {
        if (_editingAddress)
        {
            UpdateAddressInput();
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
            _selected = (_selected + 1) % Options.Length;
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
            _selected = (_selected + Options.Length - 1) % Options.Length;
        if (!Raylib.IsKeyPressed(KeyboardKey.Enter) && !Raylib.IsKeyPressed(KeyboardKey.Space))
            return;

        Notice = null;
        switch (_selected)
        {
            case 0:
                game.StartSession(new HostSession(networked: false));
                break;
            case 1:
                game.StartSession(new HostSession(networked: true));
                break;
            case 2:
                _editingAddress = true;
                break;
            case 3:
                game.Quit();
                break;
        }
    }

    private void UpdateAddressInput()
    {
        for (int c = Raylib.GetCharPressed(); c > 0; c = Raylib.GetCharPressed())
            if (_address.Length < 40 && c is >= 32 and < 127)
                _address += (char)c;
        if (
            (
                Raylib.IsKeyPressed(KeyboardKey.Backspace)
                || Raylib.IsKeyPressedRepeat(KeyboardKey.Backspace)
            )
            && _address.Length > 0
        )
            _address = _address[..^1];
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            _editingAddress = false;
        if (Raylib.IsKeyPressed(KeyboardKey.Enter) && _address.Length > 0)
        {
            _editingAddress = false;
            game.StartSession(CreateClient(_address));
        }
    }

    /// <summary>Accepts "host" or "host:port".</summary>
    public static ClientSession CreateClient(string address)
    {
        int port = Config.DefaultPort;
        int colon = address.LastIndexOf(':');
        if (colon > 0 && int.TryParse(address[(colon + 1)..], out int p))
        {
            port = p;
            address = address[..colon];
        }
        return new ClientSession(address, port);
    }

    public void Draw()
    {
        Renderer.DrawCentered(
            "THE BINDING OF ISAAC",
            110,
            48,
            new Color(214, 40, 40, 255),
            shadow: true
        );
        Renderer.DrawCentered("design patterns edition - co-op", 168, 20, Renderer.TextDim);

        for (int i = 0; i < Options.Length; i++)
        {
            bool sel = i == _selected;
            Renderer.DrawCentered(
                sel ? $"> {Options[i]} <" : Options[i],
                260 + i * 48,
                30,
                sel ? Color.White : Renderer.TextDim
            );
        }

        if (_editingAddress)
        {
            Raylib.DrawRectangle(0, 0, Config.ScreenW, Config.ScreenH, new Color(0, 0, 0, 200));
            Renderer.DrawCentered("Host address (ip or ip:port)", 250, 24, Renderer.Text);
            bool caret = (int)(Raylib.GetTime() * 2) % 2 == 0;
            Renderer.DrawCentered(_address + (caret ? "_" : " "), 300, 32, Color.White);
            Renderer.DrawCentered("Enter to connect, Esc to cancel", 360, 18, Renderer.TextDim);
        }

        if (Notice != null)
            Renderer.DrawCentered(Notice, Config.ScreenH - 110, 20, new Color(255, 120, 120, 255));
        Renderer.DrawCentered(
            "WASD move   Arrows shoot   Esc back to menu",
            Config.ScreenH - 50,
            18,
            Renderer.TextDim
        );
    }
}
