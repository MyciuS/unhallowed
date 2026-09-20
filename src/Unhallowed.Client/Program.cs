using System.Windows.Forms;

namespace Unhallowed.Client;

internal static class Program
{
    /// <summary>
    /// Client entry point.
    /// Usage: Unhallowed.Client.exe [serverUrl] [matchCode] [displayName].
    /// With no arguments a small join dialog is shown instead, which is the easiest way to
    /// start several clients against one server.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        string serverUrl;
        string matchCode;
        string displayName;

        if (args.Length >= 3)
        {
            serverUrl = args[0];
            matchCode = args[1];
            displayName = args[2];
        }
        else
        {
            using var connect = new ConnectForm();
            if (connect.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            serverUrl = connect.ServerUrl;
            matchCode = connect.MatchCode;
            displayName = connect.DisplayName;
        }

        Application.Run(new GameForm(serverUrl, matchCode, displayName));
    }
}
