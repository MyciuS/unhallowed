namespace Isaac.Core;

public interface IPoolable
{
    /// <summary>Called when the object is returned to the pool.</summary>
    void Reset();
}

/// <summary>
/// OBJECT POOL pattern: reuses short-lived objects (tears/projectiles) instead
/// of allocating new ones every shot, which keeps GC pressure low.
/// </summary>
public sealed class ObjectPool<T> where T : class, IPoolable, new()
{
    private readonly Stack<T> _free = new();

    public ObjectPool(int prewarm = 0)
    {
        for (int i = 0; i < prewarm; i++) _free.Push(new T());
    }

    public int FreeCount => _free.Count;

    public T Get() => _free.Count > 0 ? _free.Pop() : new T();

    public void Release(T item)
    {
        item.Reset();
        _free.Push(item);
    }
}
