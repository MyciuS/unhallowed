namespace Unhallowed.Core.Common;

// PATTERN: Singleton -> docs/patterns/01-singleton.md
// Specific requirements covered here: private constructor, sealed class, thread-safe lazy
// initialisation, single global access point, and a Reset hook so unit tests are not poisoned
// by state leaking between test cases.

/// <summary>
/// The one place long-lived game services are registered and resolved.
/// Exactly one registry exists per process.
/// </summary>
public sealed class ServiceRegistry
{
    private static readonly Lazy<ServiceRegistry> _instance =
        new(() => new ServiceRegistry(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Dictionary<Type, object> _services = [];
    private readonly Lock _gate = new();

    /// <summary>Private: callers must go through <see cref="Instance"/>.</summary>
    private ServiceRegistry()
    {
    }

    /// <summary>The single global access point.</summary>
    public static ServiceRegistry Instance => _instance.Value;

    public void Register<TService>(TService service)
        where TService : class
    {
        lock (_gate)
        {
            _services[typeof(TService)] = service;
        }
    }

    public TService Resolve<TService>()
        where TService : class
    {
        lock (_gate)
        {
            return _services.TryGetValue(typeof(TService), out var service)
                ? (TService)service
                : throw new InvalidOperationException(
                    $"Service '{typeof(TService).Name}' was never registered.");
        }
    }

    public bool TryResolve<TService>(out TService? service)
        where TService : class
    {
        lock (_gate)
        {
            if (_services.TryGetValue(typeof(TService), out var found))
            {
                service = (TService)found;
                return true;
            }

            service = null;
            return false;
        }
    }

    /// <summary>Clears every registration. Intended for test setup, not for gameplay code.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _services.Clear();
        }
    }
}
