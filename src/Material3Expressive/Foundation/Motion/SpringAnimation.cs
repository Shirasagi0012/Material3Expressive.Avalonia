using System;

namespace Material3Expressive.Foundation.Motion;

/// <summary>
/// A single spring-driven value: retargetable mid-flight, velocity-preserving, and independent of
/// how often it is sampled.
/// </summary>
/// <remarks>
/// This is the runtime half of <see cref="SpringSpec" />, modelled on Compose's <c>Animatable</c>.
/// The distinguishing behaviour, and the reason Avalonia's own transitions cannot express Material
/// 3 Expressive motion, is retargeting: interrupting a spring does not restart it from rest. The
/// current value <i>and velocity</i> become the new starting conditions, so a button released
/// mid-press continues from wherever it had got to, at the speed it was already moving. A
/// duration-and-easing transition has to snap or restart.
/// <para>
/// Not thread-safe; drive it from the UI thread.
/// </para>
/// </remarks>
public sealed class SpringAnimation : IAnimation
{
    private SpringSimulation _simulation;
    private double _startValue;
    private double _startVelocity;
    private double _elapsedSeconds;
    private double _durationSeconds;

    public SpringAnimation(double value, SpringSpec spec)
    {
        Spec = spec;
        _simulation = new SpringSimulation(spec);
        Value = value;
        Target = value;
        _startValue = value;
    }

    /// <summary>The spring's current value.</summary>
    public double Value { get; private set; }

    /// <summary>The current rate of change, in units per second.</summary>
    public double Velocity { get; private set; }

    /// <summary>Where the spring is heading.</summary>
    public double Target { get; private set; }

    public SpringSpec Spec { get; private set; }

    /// <summary>True while the spring has not yet settled.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// How long the current leg is expected to take. Settling is decided by this estimate rather
    /// than by watching for a small value, so an animation ends at the same moment regardless of
    /// frame rate — and so an overshoot that passes through the target is not mistaken for arrival.
    /// </summary>
    public double DurationSeconds => _durationSeconds;

    /// <summary>Jumps to a value, cancelling any motion.</summary>
    public void SnapTo(double value)
    {
        Value = value;
        Target = value;
        Velocity = 0;
        _startValue = value;
        _startVelocity = 0;
        _elapsedSeconds = 0;
        _durationSeconds = 0;
        IsRunning = false;
    }

    /// <summary>
    /// Heads for a new target, keeping whatever velocity the spring already had.
    /// </summary>
    /// <param name="target">The new resting value.</param>
    /// <param name="spec">A spring to switch to; the current one is kept when omitted.</param>
    /// <param name="initialVelocity">
    /// Overrides the velocity carried into the new leg. Used when the caller has reframed the
    /// animation — reversing a shape morph flips progress, so the velocity has to be negated to
    /// match.
    /// </param>
    public void AnimateTo(double target, SpringSpec? spec = null, double? initialVelocity = null)
    {
        if (spec is { } replacement && !replacement.Equals(Spec))
        {
            Spec = replacement;
            _simulation = new SpringSimulation(replacement);
        }

        if (initialVelocity is { } velocity)
            Velocity = velocity;

        if (Target == target && IsRunning)
            return;

        Target = target;

        if (Value == target && Velocity == 0)
        {
            IsRunning = false;
            return;
        }

        // The current state becomes the new starting condition — this is what makes a reversal
        // continue smoothly instead of restarting from rest.
        _startValue = Value;
        _startVelocity = Velocity;
        _elapsedSeconds = 0;
        _durationSeconds = EstimateDuration();
        IsRunning = _durationSeconds > 0;

        if (!IsRunning)
            SnapTo(target);
    }

    /// <summary>
    /// Advances the spring. Returns true while it is still running.
    /// </summary>
    public bool Tick(TimeSpan delta)
    {
        if (!IsRunning)
            return false;

        _elapsedSeconds += delta.TotalSeconds;

        if (_elapsedSeconds >= _durationSeconds)
        {
            SnapTo(Target);
            return false;
        }

        var motion = _simulation.Update(_startValue, _startVelocity, Target, _elapsedSeconds);
        Value = motion.Value;
        Velocity = motion.Velocity;
        return true;
    }

    /// <summary>Jumps to the target, abandoning the remaining motion.</summary>
    public void Settle()
    {
        SnapTo(Target);
    }

    private double EstimateDuration()
    {
        var threshold = Spec.VisibilityThreshold;
        if (threshold <= 0)
            threshold = SpringSpec.DefaultVisibilityThreshold;

        var seconds = SpringEstimation.DurationSeconds(
            Spec.DampingRatio,
            Spec.Stiffness,
            (_startValue - Target) / threshold,
            _startVelocity / threshold,
            1.0);

        // An undamped spring never settles; nothing in Material is undamped, but a hand-authored
        // scheme could be, and a non-terminating animation is worse than an instant one.
        return double.IsFinite(seconds) && seconds > 0 ? seconds : 0;
    }
}
