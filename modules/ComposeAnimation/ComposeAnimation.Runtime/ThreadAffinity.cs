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

    public void Verify()
    {
        if (Environment.CurrentManagedThreadId != _thread)
            throw new InvalidOperationException("Call this animation state object from the thread that created it.");
    }
}
