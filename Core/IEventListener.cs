namespace Isaac.Core;

/// <summary>
/// OBSERVER pattern, the Observer interface: anything that wants to be told
/// about events of type <typeparamref name="T"/>. A class can implement it
/// several times to listen to several event types.
/// </summary>
public interface IEventListener<in T>
{
    /// <summary>Called by the <see cref="EventBus"/> whenever a <typeparamref name="T"/> is published.</summary>
    void OnEvent(T evt);
}
