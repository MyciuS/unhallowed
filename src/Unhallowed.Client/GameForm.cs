using System.Drawing;
using System.Windows.Forms;
using Unhallowed.Client.Net;
using Unhallowed.Client.Rendering;

namespace Unhallowed.Client;

/// <summary>
/// The game window. Requirement 8: everything is drawn with primitive GDI+ calls onto a
/// double-buffered surface - no engine, no sprite pipeline.
/// </summary>
internal sealed class GameForm : Form
{
    private const int FrameIntervalMs = 16;   // ~60 FPS render
    private const int InputIntervalMs = 33;   // ~30 Hz command send

    private readonly GameClient _client = new();
    private readonly WorldRenderer _renderer = new();
    private readonly HashSet<Keys> _held = [];
    private readonly System.Windows.Forms.Timer _frameTimer = new() { Interval = FrameIntervalMs };
    private readonly System.Windows.Forms.Timer _inputTimer = new() { Interval = InputIntervalMs };

    private readonly string _serverUrl;
    private readonly string _matchCode;
    private readonly string _displayName;

    private (float X, float Y) _lastMove = (0f, 0f);
    private (float X, float Y) _lastAim = (0f, 0f);

    public GameForm(string serverUrl, string matchCode, string displayName)
    {
        _serverUrl = serverUrl;
        _matchCode = matchCode;
        _displayName = displayName;

        Text = $"Unhallowed - {displayName} @ {matchCode}";
        ClientSize = new Size(1000, 640);
        MinimumSize = new Size(640, 420);
        BackColor = Color.Black;
        KeyPreview = true;
        DoubleBuffered = true;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer,
            true);

        _frameTimer.Tick += (_, _) => Invalidate();
        _inputTimer.Tick += async (_, _) => await SendInputAsync();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        try
        {
            await _client.ConnectAsync(_serverUrl, _matchCode, _displayName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not reach the server at {_serverUrl}.\n\n{ex.Message}\n\n" +
                "Start it with:  dotnet run --project src/Unhallowed.Server",
                "Connection failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        _frameTimer.Start();
        _inputTimer.Start();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _held.Add(e.KeyCode);
        e.Handled = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _held.Remove(e.KeyCode);
        e.Handled = true;
    }

    /// <summary>Arrow keys must reach OnKeyDown rather than moving focus between controls.</summary>
    protected override bool IsInputKey(Keys keyData) => true;

    private async Task SendInputAsync()
    {
        if (!_client.IsConnected)
        {
            return;
        }

        var move = ReadAxis(Keys.A, Keys.D, Keys.W, Keys.S);
        var aim = ReadAxis(Keys.Left, Keys.Right, Keys.Up, Keys.Down);

        // Only send when the intent actually changed - the server keeps applying the last value.
        if (move != _lastMove)
        {
            _lastMove = move;
            await _client.SendMoveAsync(move.X, move.Y);
        }

        if (aim != _lastAim)
        {
            _lastAim = aim;
            await _client.SendShootAsync(aim.X, aim.Y);
        }
    }

    private (float X, float Y) ReadAxis(Keys left, Keys right, Keys up, Keys down)
    {
        var x = (_held.Contains(right) ? 1f : 0f) - (_held.Contains(left) ? 1f : 0f);
        var y = (_held.Contains(down) ? 1f : 0f) - (_held.Contains(up) ? 1f : 0f);
        return (x, y);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // PATTERN: Adapter - wrap the WinForms Graphics in the surface the domain understands.
        using var surface = new GdiRenderSurface(e.Graphics, ClientSize.Width, ClientSize.Height);

        string[] log;
        lock (_client.EventLog)
        {
            log = [.. _client.EventLog];
        }

        _renderer.Render(
            surface,
            _client.Entities,
            _client.PlayerId,
            _client.RoomId,
            _client.ServerTick,
            log,
            _client.IsConnected);
    }

    protected override async void OnFormClosed(FormClosedEventArgs e)
    {
        _frameTimer.Stop();
        _inputTimer.Stop();
        await _client.DisposeAsync();
        base.OnFormClosed(e);
    }
}
