namespace ComposeAnimation.Runtime;

/// <summary>
/// Thread check for state objects that hold no clock or scope: the thread that constructed the
/// object owns it (in an app, the UI thread). Runs additionally check their scope.
/// </summary>
internal readonly struct ThreadAffinity
{
    private readonly int _thread;

    private ThreadAffinity(int thread) => _thread = thread;

    public static ThreadAffinity Current => new(Environment.CurrentManagedThreadId);

    /// <summary>Throws unless called on the owning thread; the message names <paramref name="owner"/>'s type.</summary>
    public void Verify(object owner)
    {
        // default(ThreadAffinity) has no thread (managed thread ids start at 1): always a bug.
        if (_thread == 0) throw new InvalidOperationException("ThreadAffinity used before it was captured.");
        if (Environment.CurrentManagedThreadId != _thread)
            throw new InvalidOperationException(
                $"Call this {owner.GetType().Name.Split('`')[0]} from the thread that created it.");
    }
}
