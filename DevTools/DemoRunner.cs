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

    /// <summary>What the finished demo printed, one entry per line.</summary>
    public IReadOnlyList<string> Lines { get; private set; } = [];

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
        Lines = task.Result.Replace("\r", "").Split('\n');
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
        catch (Exception ex)
        {
            writer.WriteLine();
            writer.WriteLine($"The demo failed: {ex}");
        }
        finally
        {
            Console.SetOut(previous);
        }
        return writer.ToString();
    }
}
