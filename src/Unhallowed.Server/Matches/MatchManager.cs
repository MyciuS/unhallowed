using System.Collections.Concurrent;

namespace Unhallowed.Server.Matches;

/// <summary>
/// Owns every live match. Registered as a DI singleton - one instance for the whole server.
/// </summary>
public sealed class MatchManager
{
    private readonly ConcurrentDictionary<string, Match> _matches = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<Match> ActiveMatches => _matches.Values.ToArray();

    /// <summary>Returns the match for <paramref name="code"/>, creating it on first use.</summary>
    public Match GetOrCreate(string code) => _matches.GetOrAdd(code, static c => new Match(c));

    public Match? Find(string code) => _matches.GetValueOrDefault(code);

    /// <summary>Finds whichever match a connection belongs to, or null.</summary>
    public Match? FindByConnection(string connectionId)
        => _matches.Values.FirstOrDefault(m => m.PlayerIdFor(connectionId) is not null);

    public void RemoveIfEmpty(string code)
    {
        if (_matches.TryGetValue(code, out var match) && match.IsEmpty)
        {
            _matches.TryRemove(code, out _);
        }
    }
}
