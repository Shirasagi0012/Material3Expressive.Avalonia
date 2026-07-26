using System;

namespace Material3Expressive.Foundation;

/// <summary>
/// A duration-and-easing animation, for the few places Material still specifies one.
/// </summary>
/// <remarks>
/// Unlike <see cref="SpringAnimation" /> a tween has no velocity to preserve, so retargeting
/// restarts from the current value. That is upstream's behaviour too — these animations are short
/// enough (15–225ms) that the discontinuity does not read.
/// </remarks>
public sealed class TweenAnimation : IAnimation
{
    private double _startValue;
    private double _elapsedSeconds;

    public TweenAnimation(double value, TimeSpan duration, Easing? easing = null)
    {
        Value = value;
        Target = value;
        _startValue = value;
        Duration = duration;
        Easing = easing ?? Easings.Linear;
    }

    public double Value { get; private set; }

    public double Target { get; private set; }

    public TimeSpan Duration { get; private set; }

    public Easing Easing { get; private set; }

    public bool IsRunning { get; private set; }

    public void SnapTo(double value)
    {
        Value = value;
        Target = value;
        _startValue = value;
        _elapsedSeconds = 0;
        IsRunning = false;
    }

    /// <summary>Animates to a target, optionally switching to a different duration and curve.</summary>
    public void AnimateTo(double target, TimeSpan? duration = null, Easing? easing = null)
    {
        if (duration is { } newDuration)
            Duration = newDuration;

        if (easing is { } newEasing)
            Easing = newEasing;

        if (Target == target && IsRunning)
            return;

        if (Value == target || Duration <= TimeSpan.Zero)
        {
            SnapTo(target);
            return;
        }

        _startValue = Value;
        Target = target;
        _elapsedSeconds = 0;
        IsRunning = true;
    }

    public bool Tick(TimeSpan delta)
    {
        if (!IsRunning)
            return false;

        _elapsedSeconds += delta.TotalSeconds;

        var fraction = _elapsedSeconds / Duration.TotalSeconds;
        if (fraction >= 1)
        {
            SnapTo(Target);
            return false;
        }

        Value = _startValue + (Target - _startValue) * Easing(fraction);
        return true;
    }

    public void Settle()
    {
        SnapTo(Target);
    }
}
