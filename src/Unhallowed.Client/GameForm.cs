using System.Drawing;
using System.Windows.Forms;
using Unhallowed.Client.Net;
using Unhallowed.Client.Rendering;

namespace Unhallowed.Client;

/// <summary>
/// The game window. Everything is drawn with primitive GDI+ calls onto a double-buffered
/// surface - no engine, no sprite pipeline. Input is sampled and sent as a movement intent;
/// the window only ever renders the snapshots the server sends back.
/// </summary>
internal sealed class GameForm : Form
{
    private const int FrameIntervalMs = 16;   // ~60 FPS render
    private const int InputIntervalMs = 33;   // ~30 Hz intent send

    private readonly GameClient _client = new();
    private readonly WorldRenderer _renderer = new();
    private readonly HashSet<Keys> _held = [];
    private readonly System.Windows.Forms.Timer _frameTimer = new() { Interval = FrameIntervalMs };
    private readonly System.Windows.Forms.Timer _inputTimer = new() { Interval = InputIntervalMs };

    private readonly string _serverUrl;
    private readonly string _matchCode;
    private readonly string _displayName;

    private (float X, float Y) _lastMove = (0f, 0f);

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

        var right = _held.Contains(Keys.D) || _held.Contains(Keys.Right);
        var left = _held.Contains(Keys.A) || _held.Contains(Keys.Left);
        var down = _held.Contains(Keys.S) || _held.Contains(Keys.Down);
        var up = _held.Contains(Keys.W) || _held.Contains(Keys.Up);

        var move = (
            X: (right ? 1f : 0f) - (left ? 1f : 0f),
            Y: (down ? 1f : 0f) - (up ? 1f : 0f));

        // Only send when the intent actually changed - the server keeps applying the last value.
        if (move != _lastMove)
        {
            _lastMove = move;
            await _client.SendMoveAsync(move.X, move.Y);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        string[] log;
        lock (_client.EventLog)
        {
            log = [.. _client.EventLog];
        }

        _renderer.Render(
            e.Graphics,
            ClientSize,
            _client.Players,
            _client.PlayerId,
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
