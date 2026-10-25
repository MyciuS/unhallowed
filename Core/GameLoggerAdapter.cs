using Microsoft.Extensions.Logging;

namespace Isaac.Core;

/// <summary>ADAPTER: translates Microsoft's ILogger contract into GameLogger.Log(string).</summary>
public sealed class GameLoggerAdapter(GameLogger logger, string categoryName) : ILogger
{
    // GameLogger writes independent lines and has no scope support.
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        if (exception != null)
            message += Environment.NewLine + exception;

        logger.Log($"[{logLevel}] {categoryName} ({eventId.Id}): {message}");
    }
}
