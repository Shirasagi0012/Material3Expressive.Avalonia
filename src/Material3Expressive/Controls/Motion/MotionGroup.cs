using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// Drives a control's animations off the render clock.
/// </summary>
/// <remarks>
/// One group per control, holding every value that control animates. It runs on
/// <see cref="TopLevel.RequestAnimationFrame" />, which is a single-shot request: the group
/// re-requests only while something is still moving, so an idle control costs nothing and a settled
/// animation stops the loop rather than idling on a timer.
/// <para>
/// A detached control has no <c>TopLevel</c> and therefore no clock. Rather than leave animations
/// frozen part-way, the group settles them — the visual result is the one they were heading for,
/// which is what a control re-attached later should show.
/// </para>
/// </remarks>
public sealed class MotionGroup
{
    private readonly Visual _host;
    private readonly List<IAnimation> _animations = new();
    private readonly Action<TimeSpan> _onFrame;

    private TimeSpan? _lastFrame;
    private bool _manualClock;
    private bool _scheduled;

    public MotionGroup(Visual host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _onFrame = OnFrame;
        MotionTestClockScope.Register(this);
    }

    /// <summary>Raised once per frame, after every animation in the group has advanced.</summary>
    public event EventHandler? Ticked;

    /// <summary>True while anything in the group is moving.</summary>
    public bool IsRunning
    {
        get
        {
            foreach (var animation in _animations)
            {
                if (animation.IsRunning)
                    return true;
            }

            return false;
        }
    }

    /// <summary>Puts an animation under this group's clock.</summary>
    public T Add<T>(T animation) where T : IAnimation
    {
        _animations.Add(animation ?? throw new ArgumentNullException(nameof(animation)));
        return animation;
    }

    /// <summary>Creates a spring in this group, resting at <paramref name="value" />.</summary>
    public SpringAnimation AddSpring(double value, SpringSpec spec)
    {
        return Add(new SpringAnimation(value, spec));
    }

    /// <summary>Creates a tween in this group, resting at <paramref name="value" />.</summary>
    public TweenAnimation AddTween(double value, TimeSpan duration, Easing? easing = null)
    {
        return Add(new TweenAnimation(value, duration, easing));
    }

    /// <summary>
    /// Starts the frame loop if it is not already running. Call after retargeting anything.
    /// </summary>
    public void Start()
    {
        if (_manualClock || _scheduled || !IsRunning)
            return;

        if (TopLevel.GetTopLevel(_host) is not { } topLevel)
        {
            // Initial styled-property values are commonly assigned before the control reaches a
            // visual tree. Keep their running state intact: MaterialControl will retry Start as
            // soon as a TopLevel exists, while its logical-detach path remains responsible for
            // settling a genuinely detached control.
            return;
        }

        _scheduled = true;
        _lastFrame = null;
        topLevel.RequestAnimationFrame(_onFrame);
    }

    /// <summary>Jumps every animation to its end and stops the loop.</summary>
    public void SettleAll()
    {
        var changed = false;

        foreach (var animation in _animations)
        {
            if (!animation.IsRunning)
                continue;

            animation.Settle();
            changed = true;
        }

        if (changed)
            Ticked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Switches this group from the render clock to explicit test-driven time.
    /// </summary>
    /// <remarks>
    /// Internal on purpose: production controls must use the compositor clock, while visual tests
    /// need repeatable intermediate frames independent of host speed.
    /// </remarks>
    internal void UseManualClock()
    {
        _manualClock = true;
        _scheduled = false;
        _lastFrame = null;
    }

    /// <summary>Advances a manually clocked group by an exact duration.</summary>
    internal void AdvanceBy(TimeSpan delta)
    {
        if (!_manualClock)
            throw new InvalidOperationException("Call UseManualClock before advancing a motion group.");
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta), "Animation time cannot move backwards.");

        // Indexed rather than foreach: a tick handler may add a transient ripple.
        for (var i = 0; i < _animations.Count; i++)
            _animations[i].Tick(delta);

        Ticked?.Invoke(this, EventArgs.Empty);
    }

    private void OnFrame(TimeSpan now)
    {
        // A frame may already have been queued when a visual test takes over the clock.
        if (_manualClock)
        {
            _scheduled = false;
            _lastFrame = null;
            return;
        }

        _scheduled = false;

        // The clock hands out an absolute time, so a freshly started loop has no delta yet.
        var delta = _lastFrame is { } last ? now - last : TimeSpan.Zero;
        _lastFrame = now;

        var running = false;

        // Indexed rather than foreach: an animation's Ticked handler may add another.
        for (var i = 0; i < _animations.Count; i++)
            running |= _animations[i].Tick(delta);

        Ticked?.Invoke(this, EventArgs.Empty);

        // A handler may have started something new, so ask the group rather than trusting the ticks.
        if (!running && !IsRunning)
            return;

        if (TopLevel.GetTopLevel(_host) is not { } topLevel)
        {
            SettleAll();
            return;
        }

        _scheduled = true;
        topLevel.RequestAnimationFrame(_onFrame);
    }

    /// <summary>Drops an animation from the group — for ripples, which are transient.</summary>
    public void Remove(IAnimation animation)
    {
        _animations.Remove(animation);
    }
}
