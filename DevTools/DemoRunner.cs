using Isaac.Demos;

namespace Isaac.DevTools;

/// <summary>Runs a console pattern demo in the background and keeps what it printed.</summary>
public sealed class DemoRunner
{
    private Task<string>? _task;

    /// <summary>The demo that is running now, or null.</summary>
    public string? Running { get; private set; }

    /// <summary>The demo whose output is in <see cref="Output"/>.</summary>
    public string? Finished { get; private set; }

    public string Output { get; private set; } = "";

    public void Start(string demo)
    {
        if (Running != null) return;
        Running = demo;
        // Off the game thread: some demos (the prototype benchmark) take a second or two.
        _task = Task.Run(() => Capture(() => PatternDemos.Run(demo)));
    }

    /// <summary>Picks up the result once the background demo has finished. Call every frame.</summary>
    public void Poll()
    {
        if (_task is not { IsCompleted: true } task || Running == null) return;
        Output = task.IsFaulted ? task.Exception!.GetBaseException().ToString() : task.Result;
        Finished = Running;
        Running = null;
        _task = null;
    }

    private static string Capture(Action demo)
    {
        // Console output is process wide, so game log lines printed meanwhile end up here too;
        // the synchronized writer at least keeps them from corrupting each other.
        var writer = new StringWriter();
        var previous = Console.Out;
        Console.SetOut(TextWriter.Synchronized(writer));
        try
        {
            demo();
        }
        finally
        {
            Console.SetOut(previous);
        }
        return writer.ToString();
    }
}
