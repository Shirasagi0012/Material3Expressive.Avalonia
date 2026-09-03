namespace Material3ExpressiveRefactor.Foundation.Rendering.Symbols;

/// <summary>
/// A small bounded least-recently-used cache, safe to share between the UI thread and the render
/// thread.
/// </summary>
/// <remarks>
/// <para>
/// Evicted values are dropped, never disposed. A render-thread caller can still be holding a value
/// at the moment a UI-thread insert evicts it, and disposing a live SkiaSharp handle out from under
/// it would be a use-after-free. SkiaSharp's native objects carry finalizers, so the handle is
/// released once the last managed reference goes away — a bounded cache plus GC is the safe trade.
/// </para>
/// <para>
/// The value factory runs while the lock is held. That serializes cold misses, which
/// is deliberate: it keeps two threads from building the same variable-font instance twice, and the
/// cache is warm within one animation sweep.
/// </para>
/// </remarks>
internal sealed class LruCache<TKey, TValue>(int capacity)
    where TKey : notnull
{
    private readonly record struct Entry(TKey Key, TValue Value);

    private readonly Lock _gate = new();
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _index = new(capacity);
    private readonly LinkedList<Entry> _order = new();

    private long _hits;
    private long _misses;

    /// <summary>Number of lookups served from the cache.</summary>
    public long Hits { get { lock (_gate) return _hits; } }

    /// <summary>Number of lookups that had to build a value.</summary>
    public long Misses { get { lock (_gate) return _misses; } }

    /// <summary>Number of values currently held.</summary>
    public int Count { get { lock (_gate) return _index.Count; } }

    /// <summary>Returns the cached value for <paramref name="key"/>, building it on a miss.</summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) =>
        GetOrAdd(key, static (k, factory) => factory(k), valueFactory);

    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, building it on a miss. The
    /// <paramref name="state"/> overload lets callers on the render path pass a static factory and
    /// avoid allocating a closure per lookup.
    /// </summary>
    public TValue GetOrAdd<TState>(TKey key, Func<TKey, TState, TValue> valueFactory, TState state)
    {
        lock (_gate)
        {
            if (_index.TryGetValue(key, out var existing))
            {
                _hits++;
                _order.Remove(existing);
                _order.AddFirst(existing);
                return existing.Value.Value;
            }

            _misses++;
            var value = valueFactory(key, state);
            var node = _order.AddFirst(new Entry(key, value));
            _index[key] = node;

            while (_index.Count > capacity)
            {
                var evicted = _order.Last!;
                _order.RemoveLast();
                _index.Remove(evicted.Value.Key);
            }

            return value;
        }
    }

    /// <summary>Zeroes the hit/miss counters. Does not evict.</summary>
    public void ResetStatistics()
    {
        lock (_gate)
        {
            _hits = 0;
            _misses = 0;
        }
    }

    /// <summary>Drops every entry.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _index.Clear();
            _order.Clear();
        }
    }
}
