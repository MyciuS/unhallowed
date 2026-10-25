using Microsoft.Extensions.Logging;

namespace Isaac.Core;

/// <summary>Connects the adapter to the framework's logging pipeline.</summary>
public sealed class GameLoggerProvider(GameLogger logger) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new GameLoggerAdapter(logger, categoryName);

    public void Dispose()
    {
        // The shared singleton belongs to the game, not this provider.
    }
}
