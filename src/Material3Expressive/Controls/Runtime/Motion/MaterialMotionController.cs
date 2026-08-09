using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// The owner-local flat animation scheduler.
/// </summary>
/// <remarks>
/// Registration is deliberately separate from scheduling: owners add an animation once, request
/// a frame after a retarget, and remove the animation when its own lifetime ends. This controller
/// never infers ownership or automatically removes an idle track.
/// </remarks>
internal sealed class MaterialMotionController
{
    private readonly Visual _owner;
    private readonly List<IAnimation> _animations = [];
    private bool _active;
    private bool _manualClock;
    private bool _scheduled;
    private TimeSpan? _lastFrame;
    private long _epoch;

    internal MaterialMotionController(Visual owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        MotionTestClockScope.Register(this);
    }

    /// <summary>Runs after tracks tick and before the owner synchronizes its animated values.</summary>
    internal event Action? BeforeOwnerFrame;

    /// <summary>Runs after the owner synchronizes values and before it is invalidated.</summary>
    internal event Action? AfterOwnerFrame;

    internal bool IsRunning => AnyRunning();

    internal void Activate()
    {
        if (_active)
            return;

        _active = true;
        _owner.AttachedToVisualTree += OnAttachedToVisualTree;
        _owner.DetachedFromVisualTree += OnDetachedFromVisualTree;
        if (_owner.IsAttachedToVisualTree())
            RequestFrame();
    }

    internal T Add<T>(T animation) where T : IAnimation
    {
        ArgumentNullException.ThrowIfNull(animation);
        _animations.Add(animation);
        return animation;
    }

    internal SpringAnimation AddSpring(double value, SpringSpec spec) =>
        Add(new SpringAnimation(value, spec));

    internal TweenAnimation AddTween(double value, TimeSpan duration, Easing? easing = null) =>
        Add(new TweenAnimation(value, duration, easing));

    internal bool Remove(IAnimation animation) => _animations.Remove(animation);

    /// <summary>Ensures a compositor callback exists if at least one registered track is running.</summary>
    internal void RequestFrame()
    {
        if (_manualClock || _scheduled || !AnyRunning())
            return;

        if (TopLevel.GetTopLevel(_owner) is not { } topLevel)
            return;

        _scheduled = true;
        var scheduledEpoch = _epoch;
        topLevel.RequestAnimationFrame(now => OnFrame(scheduledEpoch, now));
    }

    /// <summary>Cancels a queued compositor callback without changing animation ownership or values.</summary>
    internal void CancelScheduledCallbacksAndSettle()
    {
        _epoch++;
        _scheduled = false;
        _lastFrame = null;
    }

    /// <summary>Settles running tracks even when owner-frame callbacks retarget a track.</summary>
    internal void SettleAllForLifecycle()
    {
        if (!SettleAnimations())
            return;

        // This is deliberately one visual transaction. A callback may start another animation;
        // settle that new value too, but do not publish a second intermediate owner frame.
        PublishOwnerFrame();
        while (SettleAnimations())
        {
        }
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
            throw new InvalidOperationException("Call UseManualClock before advancing a material controller.");
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta), "Animation time cannot move backwards.");

        TickAnimations(delta);
        PublishOwnerFrame();
    }

    internal void SettleAll()
    {
        if (SettleAnimations())
            PublishOwnerFrame();
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => RequestFrame();

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e) =>
        CancelScheduledCallbacksAndSettle();

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
        var delta = _lastFrame is { } last ? now - last : TimeSpan.Zero;
        _lastFrame = now;

        TickAnimations(delta);
        PublishOwnerFrame();

        // Owner and observer callbacks are allowed to retarget or add tracks. Always inspect
        // the final state, rather than the state from before those callbacks ran.
        if (!AnyRunning())
        {
            _lastFrame = null;
            return;
        }

        RequestFrame();
    }

    private void TickAnimations(TimeSpan delta)
    {
        // Index against the live list so a callback can remove the just-ticked animation without
        // skipping its successor. Newly added tracks can participate in this transaction.
        for (var index = 0; index < _animations.Count;)
        {
            var animation = _animations[index];
            animation.Tick(delta);
            if (index < _animations.Count && ReferenceEquals(_animations[index], animation))
                index++;
        }
    }

    private bool SettleAnimations()
    {
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

    private void PublishOwnerFrame()
    {
        BeforeOwnerFrame?.Invoke();
        if (_owner is IMaterialMotionConsumer consumer)
            consumer.OnMaterialMotionFrame();
        AfterOwnerFrame?.Invoke();
        _owner.InvalidateVisual();
    }

    private bool AnyRunning()
    {
        for (var index = 0; index < _animations.Count; index++)
        {
            if (_animations[index].IsRunning)
                return true;
        }

        return false;
    }
}
