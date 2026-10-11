namespace Isaac.Core;

/// <summary>
/// SINGLETON pattern: one log for the whole program, reachable from anywhere
/// through <see cref="Instance"/>. Writes timestamped lines to the console and game.log.
///
/// Thread safety matters here: SignalR hub callbacks run on thread pool threads
/// while the game loop runs on the main thread, and both log.
/// - <see cref="Lazy{T}"/> guarantees the instance is created exactly once, even if
///   two threads ask for it at the same moment.
/// - A lock around each write keeps lines from interleaving in the file.
/// </summary>
public sealed class GameLogger
{
    public const string FileName = "game.log";

    private static readonly Lazy<GameLogger> LazyInstance = new(() => new GameLogger());

    public static GameLogger Instance => LazyInstance.Value;

    private readonly object _lock = new();
    private readonly StreamWriter? _file;

    private GameLogger()
    {
        try
        {
            _file = new StreamWriter(FileName, append: true) { AutoFlush = true };
        }
        catch (IOException)
        {
            // Log file locked (e.g. a second copy of the game is running): console only.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public void Log(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        lock (_lock)
        {
            Console.WriteLine(line);
            _file?.WriteLine(line);
        }
    }
}
