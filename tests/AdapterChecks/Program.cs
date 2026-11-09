using Isaac.Core;
using Microsoft.Extensions.Logging;

var originalOutput = Console.Out;
using var output = new StringWriter();
try
{
    Console.SetOut(output);
    using (var factory = LoggerFactory.Create(builder =>
    {
        builder.ClearProviders();
        builder.SetMinimumLevel(LogLevel.Warning);
        builder.AddProvider(new GameLoggerProvider(GameLogger.Instance));
    }))
    {
        var logger = factory.CreateLogger("AdapterChecks.Network");
        logger.LogInformation("Filtered message");
        logger.LogWarning(new EventId(42), "Connection {Id} lost", 7);
        logger.LogError(new InvalidOperationException("Connection failure"), "Cannot reconnect");
        using (logger.BeginScope("A scope"))
            logger.LogWarning("Scoped message");
    }

    var adapter = new GameLoggerAdapter(GameLogger.Instance, "Direct");
    bool formatterCalled = false;
    adapter.Log(LogLevel.None, default, "Ignored", null, (state, exception) =>
    {
        formatterCalled = true;
        return state;
    });

    // Disposing a framework provider must not close the game's singleton logger.
    GameLogger.Instance.Log("Logger still usable");
    var text = output.ToString();
    Check(text.Contains("[Warning] AdapterChecks.Network (42): Connection 7 lost"), "structured message and metadata");
    Check(text.Contains("Cannot reconnect") && text.Contains("InvalidOperationException: Connection failure"), "exception details");
    Check(!text.Contains("Filtered message"), "framework minimum-level filter");
    Check(text.Contains("Scoped message"), "logging within a scope");
    Check(!formatterCalled && !adapter.IsEnabled(LogLevel.None), "disabled level skips formatting");
    Check(text.Contains("Logger still usable"), "singleton lifetime after provider disposal");
}
finally
{
    Console.SetOut(originalOutput);
}
Console.WriteLine("Passed 6 adapter checks.");

static void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"Failed: {name}");
}
