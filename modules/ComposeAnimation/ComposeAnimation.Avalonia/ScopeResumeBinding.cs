using Avalonia;
using Avalonia.Threading;

namespace ComposeAnimation;

/// <summary>
/// The single D17 rule for the library's <see cref="IAnimationScopeHost"/>s
/// (<see cref="AnimatedVisibility"/>, <see cref="AnimatedContent"/>, <see cref="AnimatedContentSize"/>,
/// <see cref="LookaheadSize"/>, <see cref="LookaheadPosition"/>, <see cref="AnimatedBounds"/>): a scope
/// resource that tells a rebuild (resume pending) from a final end (release), and <see cref="Handle"/>,
/// which runs the host's reaction in one fixed order. Also the deferred loop launch used by the
/// transition-loop hosts. <c>Animated.Properties</c> does not use it; see AnimatedHostCoordinator.
/// </summary>
internal sealed class ScopeResumeBinding
{
    private readonly Visual _owner;
    private readonly Func<bool> _isAttached;
    private readonly Action _onLost;
    private IDisposable? _resource;
    private bool _resumePending;

    /// <param name="owner">The host control, used to detect an ending environment.</param>
    /// <param name="isAttached">Whether the host is still in the visual tree.</param>
    /// <param name="onLost">
    /// The host's release when its scope ends for good while no resume is possible: the control is
    /// detached (and did not come back before the drain) or the window environment is ending. It
    /// drops the kept animation state (and, for transition-loop hosts, marks the host failed and
    /// disconnects). Called from the scope's resource callback, before the scope cancels its runs.
    /// </param>
    public ScopeResumeBinding(Visual owner, Func<bool> isAttached, Action onLost)
    {
        _owner = owner;
        _isAttached = isAttached;
        _onLost = onLost;
    }

    /// <summary>Watches <paramref name="motion"/> so its end either queues a resume or ends the host.</summary>
    /// <remarks>
    /// Detach does not reset the host (D17): the scope ends at the next drain if the control
    /// stays detached (this callback disconnects then), or is rebuilt if it was re-attached
    /// elsewhere (OnScopeChanged re-runs the kept state).
    /// </remarks>
    public void Bind(AnimationScope motion)
    {
        // One watched scope at a time: a previous registration is dropped first, and a callback
        // that is not for the current registration (a dispose, or a stale scope) does nothing.
        var previous = _resource;
        _resource = null;
        previous?.Dispose();
        IDisposable? registration = null;
        registration = motion.RegisterResource(() =>
        {
            if (!motion.IsEnding || !ReferenceEquals(_resource, registration)) return;
            _resource = null;
            // Still attached: the scope is being rebuilt; keep the state for OnScopeChanged (D17).
            if (_isAttached() && !AnimationScope.IsEnvironmentEnding(_owner))
            {
                _resumePending = true;
                return;
            }
            _onLost();
        });
        _resource = registration;
    }

    /// <summary>
    /// The host's whole D17 reaction to <see cref="IAnimationScopeHost.OnScopeChanged"/>, in one order:
    /// take the pending resume; on an end (no new scope) call <paramref name="onLost"/> if a resume was
    /// pending and stop; otherwise, if a resume is pending, bind the new scope and call
    /// <paramref name="onResume"/> (it returns false when the kept state cannot resume, and the binding
    /// is dropped); finally call <paramref name="invalidate"/> with whether it resumed.
    /// </summary>
    public void Handle(AnimationScopeChangedEventArgs e, Func<AnimationScope, bool> onResume, Action onLost,
        Action<bool>? invalidate = null)
    {
        var resume = TakeResume();
        if (e.NewScope is not AnimationScope motion)
        {
            if (resume) onLost();
            return;
        }
        var resumed = false;
        if (resume)
        {
            Bind(motion);
            resumed = onResume(motion);
            if (!resumed) Reset();
        }
        invalidate?.Invoke(resumed);
    }

    /// <summary>Returns and clears the pending-resume flag.</summary>
    public bool TakeResume()
    {
        var resume = _resumePending;
        _resumePending = false;
        return resume;
    }

    /// <summary>Drops the scope resource and any pending resume.</summary>
    public void Reset()
    {
        _resumePending = false;
        var resource = _resource;
        _resource = null;
        resource?.Dispose();
    }

    /// <summary>
    /// Launches <paramref name="run"/> in <paramref name="motion"/>. A resume (<paramref name="immediate"/>)
    /// launches now; a first start queues behind the layout barrier (Loaded), then launches at
    /// Normal so awaits in the frame loop don't inherit Loaded. <paramref name="isCurrent"/> is the
    /// one staleness guard, checked at each stage together with the scope's end.
    /// </summary>
    public static void StartLoop(AnimationScope motion, Func<bool> isCurrent,
        Func<CancellationToken, Task> run, string label, bool immediate)
    {
        if (immediate)
        {
            _ = motion.Launch(run, label);
            return;
        }
        Dispatcher.UIThread.Post(() =>
        {
            if (!isCurrent() || motion.IsEnding) return;
            Dispatcher.UIThread.Post(() =>
            {
                if (isCurrent() && !motion.IsEnding) _ = motion.Launch(run, label);
            }, DispatcherPriority.Normal);
        }, DispatcherPriority.Loaded);
    }
}
