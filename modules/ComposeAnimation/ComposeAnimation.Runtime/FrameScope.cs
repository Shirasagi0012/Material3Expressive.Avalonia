namespace ComposeAnimation.Runtime;

/// <summary>
/// Where an animation run executes: the scope supplies its frames, lifetime and duration scale.
/// Runtime state objects (<see cref="Animatable{T}"/>, transitions) hold state only; each
/// run names the scope it runs in. The only implementation is ComposeAnimation's
/// <c>AnimationScope</c>; the constructor is internal so user code cannot derive from it.
/// </summary>
public abstract class FrameScope : IFrameWaitProvider
{
    internal FrameScope() { }

    /// <summary>Cancelled when the scope ends. Runs in this scope are cancelled with it.</summary>
    public abstract CancellationToken Lifetime { get; }

    /// <summary>
    /// Playback duration multiplier of the scope. Raw frame timestamps are unchanged; runs divide
    /// frame deltas by the effective scale (this value times the ancestors' and the host's), and 0
    /// snaps runs to their end.
    /// </summary>
    public abstract double DurationScale { get; set; }

    /// <summary>
    /// Waits for the window's next frame. Cancelled by <paramref name="cancellationToken"/> or when
    /// the scope ends. Not async: the continuation resumes inline inside the frame batch.
    /// </summary>
    public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken = default)
        => WaitFrameAsync(cancellationToken, null);

    // ---- What runs need; internal so FrameScope stays minimal (stage 3, Q-A) ----

    /// <summary>The frame wait itself (no async layer). <paramref name="onFault"/> sees failures innermost first.</summary>
    internal abstract ValueTask<TimeSpan> WaitFrameAsync(CancellationToken cancellationToken, Action<Exception>? onFault);

    /// <summary>The scale runs apply: the chain value times the host's attached value.</summary>
    internal abstract double EffectiveDurationScale { get; }

    /// <summary>Thread check plus "the scope has not ended" (<see cref="ObjectDisposedException"/>).</summary>
    internal abstract void VerifyAccess();

    /// <summary>True once the scope has started ending; new work must not start.</summary>
    internal abstract bool IsEnding { get; }

    /// <summary>Counts <paramref name="run"/> as an active run of this scope until it completes.</summary>
    internal abstract Task<T> Track<T>(Func<Task<T>> run);

    /// <inheritdoc cref="Track{T}(Func{Task{T}})"/>
    internal abstract Task Track(Func<Task> run);

    /// <summary>Reports a run failure that no awaiting caller observes.</summary>
    internal abstract void ReportRunFailure(Exception error);

    /// <summary>
    /// Calls <paramref name="changed"/> when the effective duration scale may have changed, until
    /// disposed or the scope ends. <paramref name="owner"/> is held weakly.
    /// </summary>
    internal abstract IDisposable RegisterPlaybackChanged<T>(T owner, Action<T> changed) where T : class;

    /// <summary>Lets frame-driven helpers wait through this scope (same as <see cref="WaitFrameAsync"/>).</summary>
    ValueTask<TimeSpan> IFrameWaitProvider.NextFrameAsync(CancellationToken cancellationToken, Action<Exception>? onFault)
        => WaitFrameAsync(cancellationToken, onFault);
}
