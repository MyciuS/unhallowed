namespace Unhallowed.Core.Commands;

/// <summary>
/// Per-player inbox. The hub writes to it from arbitrary SignalR threads; the fixed-step
/// simulation loop drains it once per tick, which is what keeps the world single-threaded.
/// </summary>
public sealed class CommandQueue
{
    private readonly Queue<IGameCommand> _pending = new();
    private readonly Lock _gate = new();

    /// <summary>Highest sequence number accepted so far, echoed back to the client for ack.</summary>
    public int LastAcceptedSequence { get; private set; }

    public void Enqueue(IGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (_gate)
        {
            // Late or duplicated packets are dropped rather than applied out of order.
            if (command.Sequence <= LastAcceptedSequence)
            {
                return;
            }

            LastAcceptedSequence = command.Sequence;
            _pending.Enqueue(command);
        }
    }

    /// <summary>Removes and returns everything queued since the previous tick.</summary>
    public IReadOnlyList<IGameCommand> Drain()
    {
        lock (_gate)
        {
            if (_pending.Count == 0)
            {
                return [];
            }

            var drained = _pending.ToArray();
            _pending.Clear();
            return drained;
        }
    }
}
