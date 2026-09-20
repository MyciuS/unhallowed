using System.Drawing;
using System.Windows.Forms;

namespace Unhallowed.Client;

/// <summary>Tiny launcher so four people can join the same lobby without editing any config.</summary>
internal sealed class ConnectForm : Form
{
    private readonly TextBox _server = new() { Text = "http://localhost:5080" };
    private readonly TextBox _match = new() { Text = "default" };
    private readonly TextBox _name = new() { Text = Environment.UserName };

    public ConnectForm()
    {
        Text = "Unhallowed - join a match";
        ClientSize = new Size(360, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(14),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        }

        AddRow(layout, "Server", _server, 0);
        AddRow(layout, "Match code", _match, 1);
        AddRow(layout, "Name", _name, 2);

        var join = new Button { Text = "Join", Dock = DockStyle.Fill };
        join.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        layout.Controls.Add(new Label(), 0, 3);
        layout.Controls.Add(join, 1, 3);

        Controls.Add(layout);
        AcceptButton = join;
    }

    public string ServerUrl => _server.Text.Trim();

    public string MatchCode => _match.Text.Trim();

    public string DisplayName => _name.Text.Trim();

    private static void AddRow(TableLayoutPanel layout, string label, Control input, int row)
    {
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(3, 6, 3, 6);

        layout.Controls.Add(
            new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            },
            0,
            row);

        layout.Controls.Add(input, 1, row);
    }
}
