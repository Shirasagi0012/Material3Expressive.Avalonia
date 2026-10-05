namespace ComposeAnimation.Runtime;

/// <summary>
/// Ambient duration scale for suspend-driven animations (<see cref="SuspendAnimation"/> /
/// <see cref="Animatable{T}"/>), adapted from Compose <c>MotionDurationScale</c> / coroutine
/// <c>durationScale</c>. <see cref="Transition.DurationScale"/> remains the Transition-tree knob;
/// set both when the host should scale all animation drivers.
/// </summary>
public static class MotionDurationScale
{
    private static readonly AsyncLocal<double?> Current = new();

    /// <summary>
    /// Multiplier applied to wall-clock deltas when computing play time. Default is 1.
    /// Must be finite and &gt;= 0. A value of 0 snaps finite animations to their end on the next frame.
    /// </summary>
    public static double ScaleFactor
    {
        get => Current.Value ?? 1;
        set
        {
            if (!double.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Current.Value = value;
        }
    }

    /// <summary>Restores the default scale of 1 for the current async context.</summary>
    public static void Reset() => Current.Value = null;
}