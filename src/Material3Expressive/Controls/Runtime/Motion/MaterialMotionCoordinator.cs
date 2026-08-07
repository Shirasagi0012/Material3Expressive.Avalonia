using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// One owner-level frame clock shared by component, interaction, and presenter scopes.
/// </summary>
internal sealed class MaterialMotionCoordinator
{
    private readonly Visual _owner;
    private readonly Action _onOwnerFrame;
    private readonly List<MaterialMotionScope> _scopes = [];
    private bool _manualClock;
    private bool _scheduled;
    private TimeSpan? _lastFrame;
    private long _epoch;
    private bool _publishingFrameCallbacks;

    internal MaterialMotionCoordinator(
        Visual owner,
        Action onOwnerFrame)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _onOwnerFrame = onOwnerFrame ?? throw new ArgumentNullException(nameof(onOwnerFrame));
        MotionTestClockScope.Register(this);
    }

    internal int ScopeCount => _scopes.Count;
    internal bool IsRunning => AnyScopeRunning();
    internal bool IsPublishingFrameCallbacks => _publishingFrameCallbacks;

    internal MaterialMotionScope CreateScope(string diagnosticName)
    {
        var scope = new MaterialMotionScope(this, diagnosticName);
        _scopes.Add(scope);
        return scope;
    }

    internal void StartIfNeeded()
    {
        if (_manualClock || _scheduled || !AnyScopeRunning())
            return;

        if (TopLevel.GetTopLevel(_owner) is not { } topLevel)
            return;

        _scheduled = true;
        var scheduledEpoch = _epoch;
        topLevel.RequestAnimationFrame(now => OnFrame(scheduledEpoch, now));
    }

    internal void SettleAllForLifecycle()
    {
        var affected = new HashSet<MaterialMotionScope>();
        foreach (var scope in _scopes)
        {
            if (scope.SettleAnimations())
                affected.Add(scope);
        }

        if (affected.Count == 0)
            return;

        // Lifecycle settling is one transaction. A scope callback can retarget or create a new
        // animation (the same re-entrant behavior that the compositor frame path supports), but
        // there is no future compositor frame once the owner has left the tree. Publish callbacks
        // first, settle anything they started without publishing a second scope tick, and only
        // then synchronize/invalidate the owner once.
        _publishingFrameCallbacks = true;
        try
        {
            var scopes = _scopes.ToArray();
            for (var scopeIndex = 0; scopeIndex < scopes.Length; scopeIndex++)
            {
                if (affected.Contains(scopes[scopeIndex]))
                    scopes[scopeIndex].PublishTicked();
            }

            // A callback may create a scope as well as an animation. Iterate the live collection
            // and settle every running value; no callback is raised for this second pass because
            // the owner-level synchronization below is the single lifecycle publication point.
            for (var scopeIndex = 0; scopeIndex < _scopes.Count; scopeIndex++)
                _scopes[scopeIndex].SettleAnimations();
        }
        finally
        {
            _publishingFrameCallbacks = false;
        }

        _onOwnerFrame();
    }

    internal void CancelScheduledCallbacksAndSettle()
    {
        _epoch++;
        _scheduled = false;
        _lastFrame = null;
        SettleAllForLifecycle();
    }

    internal void UseManualClock()
    {
        _epoch++;
        _manualClock = true;
        _scheduled = false;
        _lastFrame = null;
    }

    internal void AdvanceBy(TimeSpan delta)
    {
        if (!_manualClock)
            throw new InvalidOperationException("Call UseManualClock before advancing a material coordinator.");
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta), "Animation time cannot move backwards.");

        TickAnimations(delta);
        PublishFrameCallbacks();
    }

    internal void SettleAll()
    {
        var affected = new HashSet<MaterialMotionScope>();
        foreach (var scope in _scopes)
        {
            if (scope.SettleAnimations())
                affected.Add(scope);
        }

        if (affected.Count > 0)
            PublishFrameCallbacks(affected);
    }

    internal void RemoveScope(MaterialMotionScope scope)
    {
        _scopes.Remove(scope);
    }

    private void OnFrame(long scheduledEpoch, TimeSpan now)
    {
        if (scheduledEpoch != _epoch)
            return;

        if (_manualClock)
        {
            _scheduled = false;
            _lastFrame = null;
            return;
        }

        _scheduled = false;
        // The clock hands out an absolute timestamp. A freshly started loop has no previous
        // timestamp, matching the legacy scheduler's first-frame contract; subsequent frames use the exact
        // elapsed compositor time. Keep the timestamp across the re-request made below rather
        // than treating every callback as a new loop.
        var delta = _lastFrame is { } last ? now - last : TimeSpan.Zero;
        _lastFrame = now;

        TickAnimations(delta);
        PublishFrameCallbacks();

        // A consumer is allowed to retarget or add an animation during its callback. Re-check the
        // scopes after callbacks rather than trusting the pre-callback Tick return values.
        if (!AnyScopeRunning())
        {
            // The next animation starts a new loop and must not consume the timestamp of this
            // completed loop as its first delta.
            _lastFrame = null;
            return;
        }

        StartIfNeeded();
    }

    private void TickAnimations(TimeSpan delta)
    {
        // Scope callbacks may add animations, so index through the current snapshot and do not
        // enumerate a collection that user code can mutate during a frame.
        var scopes = _scopes.ToArray();
        for (var scopeIndex = 0; scopeIndex < scopes.Length; scopeIndex++)
            scopes[scopeIndex].TickAnimations(delta);
    }

    private void PublishFrameCallbacks(IReadOnlySet<MaterialMotionScope>? affectedScopes = null)
    {
        _publishingFrameCallbacks = true;
        try
        {
            var scopes = _scopes.ToArray();
            for (var scopeIndex = 0; scopeIndex < scopes.Length; scopeIndex++)
            {
                if (affectedScopes is null || affectedScopes.Contains(scopes[scopeIndex]))
                    scopes[scopeIndex].PublishTicked();
            }
        }
        finally
        {
            _publishingFrameCallbacks = false;
        }

        _onOwnerFrame();
    }

    private bool AnyScopeRunning()
    {
        for (var index = 0; index < _scopes.Count; index++)
        {
            if (_scopes[index].IsRunning)
                return true;
        }

        return false;
    }
}

/// <summary>A disposable animation collection sharing its owner's coordinator clock.</summary>
internal sealed class MaterialMotionScope : IDisposable
{
    private readonly MaterialMotionCoordinator _coordinator;
    private readonly List<IAnimation> _animations = [];
    private bool _disposed;

    internal MaterialMotionScope(MaterialMotionCoordinator coordinator, string diagnosticName)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        DiagnosticName = diagnosticName ?? throw new ArgumentNullException(nameof(diagnosticName));
    }

    internal string DiagnosticName { get; }
    internal MaterialMotionCoordinator Coordinator => _coordinator;
    internal event EventHandler? Ticked;
    internal bool IsDisposed => _disposed;

    internal bool IsRunning
    {
        get
        {
            for (var index = 0; index < _animations.Count; index++)
            {
                if (_animations[index].IsRunning)
                    return true;
            }

            return false;
        }
    }

    internal T Add<T>(T animation) where T : IAnimation
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _animations.Add(animation ?? throw new ArgumentNullException(nameof(animation)));
        return animation;
    }

    internal SpringAnimation AddSpring(double value, SpringSpec spec) =>
        Add(new SpringAnimation(value, spec));

    internal TweenAnimation AddTween(double value, TimeSpan duration, Easing? easing = null) =>
        Add(new TweenAnimation(value, duration, easing));

    internal void Start() => _coordinator.StartIfNeeded();

    internal void Settle()
    {
        if (SettleAnimations())
            PublishTicked();
    }

    internal void SettleAll() => Settle();

    internal void Remove(IAnimation animation) => _animations.Remove(animation);

    internal void TickAnimations(TimeSpan delta)
    {
        if (_disposed)
            return;

        // Keep the index at the same position if a callback removes the animation it just
        // advanced. Newly-added animations remain eligible for this frame, matching the legacy
        // indexed loop while avoiding a sibling skip on removal.
        for (var index = 0; index < _animations.Count;)
        {
            var animation = _animations[index];
            animation.Tick(delta);
            if (index < _animations.Count && ReferenceEquals(_animations[index], animation))
                index++;
        }
    }

    internal void PublishTicked()
    {
        if (!_disposed)
            Ticked?.Invoke(this, EventArgs.Empty);
    }

    internal bool SettleAnimations()
    {
        if (_disposed)
            return false;

        var changed = false;
        for (var index = 0; index < _animations.Count; index++)
        {
            if (!_animations[index].IsRunning)
                continue;

            _animations[index].Settle();
            changed = true;
        }

        return changed;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _animations.Clear();
        Ticked = null;
        _coordinator.RemoveScope(this);
    }
}
