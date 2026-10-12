using Avalonia;
using Avalonia.Threading;

namespace ComposeAnimation;

/// <summary>
/// Shared D17 plumbing for scope-driven hosts (<see cref="AnimatedVisibility"/>,
/// <see cref="AnimatedContent"/>): the scope resource that notices a rebuild, the pending-resume
/// flag, and the deferred loop launch with its single staleness guard.
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
    /// <param name="onLost">Marks the host failed and disconnects when the scope ends for good.</param>
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
        _resource = motion.RegisterResource(() =>
        {
            if (!motion.IsEnding) return;
            _resource = null;
            // Still attached: the scope is being rebuilt; keep the state for OnScopeChanged (D17).
            if (_isAttached() && !AnimationScope.IsEnvironmentEnding(_owner))
            {
                _resumePending = true;
                return;
            }
            _onLost();
        });
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
        _resource?.Dispose();
        _resource = null;
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
