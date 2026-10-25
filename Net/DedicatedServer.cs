using System.Diagnostics;
using System.Runtime.InteropServices;
using Isaac.Core;

namespace Isaac.Net;

/// <summary>
/// Runs a <see cref="HostSession"/> without a window, so it works on a headless machine.
/// All players connect as clients. Stop it with Ctrl+C.
/// </summary>
public static class DedicatedServer
{
    public static void Run(int port)
    {
        using var session = new HostSession(networked: true, port, dedicated: true);
        if (session.Error != null)
        {
            Console.Error.WriteLine(session.Error);
            Environment.ExitCode = 1;
            return;
        }

        // Ctrl+C or `systemctl stop`. ASP.NET also listens to these, but only stops its web server.
        var stop = new ManualResetEventSlim();
        void OnSignal(PosixSignalContext ctx)
        {
            ctx.Cancel = true;
            stop.Set();
        }
        using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);
        using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);

        var clock = Stopwatch.StartNew();
        double last = 0;
        while (!stop.IsSet)
        {
            double now = clock.Elapsed.TotalSeconds;
            session.Update((float)(now - last), default);
            last = now;
            Thread.Sleep(1);
        }
        GameLogger.Instance.Log("Dedicated server stopped");
    }
}
