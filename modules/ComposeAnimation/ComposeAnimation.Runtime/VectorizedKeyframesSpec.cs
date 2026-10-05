using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;

/// <summary>
/// Returns the entry index such that <c>timestamps[i] &lt;= timeMillis &lt; timestamps[i + 1]</c>.
/// </summary>
internal static class KeyframeMath
{
    public static int FindEntryIndex(int[] timestamps, int timeMillis)
    {
        var index = Array.BinarySearch(timestamps, timeMillis);
        return index < 0 ? ~index - 1 : index;
    }
}

internal readonly record struct VectorizedKeyframeSpecElementInfo(double[] Value, IEasing Easing, ArcMode ArcMode);

/// <summary>
/// Animates based on the values defined at different timestamps in the duration of the animation
/// (i.e. different keyframes). Intervals without a custom easing interpolate linearly, and
/// 2-dimensional values can be animated along quarter-ellipse arcs via <see cref="ArcMode"/>.
/// </summary>
public sealed class VectorizedKeyframesSpec : IVectorizedDurationBasedAnimationSpec
{
    private readonly int[] _timestamps;
    private readonly Dictionary<int, VectorizedKeyframeSpecElementInfo> _keyframes;
    private readonly int _durationMillis;
    private readonly int _delayMillis;
    private readonly IEasing _defaultEasing;
    private readonly ArcMode _initialArcMode;

    // Lazily initialized on the first interpolation.
    private double[]? _times;
    private int[]? _arcModes;
    private ArcSpline? _arcSpline;
    private double[]? _lastInitial;
    private double[]? _lastTarget;

    public TimeSpan Delay { get; }
    public TimeSpan Duration { get; }

    internal VectorizedKeyframesSpec(
        IReadOnlyList<int> timestamps,
        IReadOnlyDictionary<int, VectorizedKeyframeSpecElementInfo> keyframes,
        TimeSpan duration,
        TimeSpan delay,
        IEasing defaultEasing,
        ArcMode initialArcMode)
    {
        ArgumentNullException.ThrowIfNull(timestamps);
        ArgumentNullException.ThrowIfNull(keyframes);
        ArgumentNullException.ThrowIfNull(defaultEasing);
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        _timestamps = [.. timestamps];
        _keyframes = new Dictionary<int, VectorizedKeyframeSpecElementInfo>(keyframes);
        _durationMillis = (int)(duration.Ticks / TimeSpan.TicksPerMillisecond);
        _delayMillis = (int)(delay.Ticks / TimeSpan.TicksPerMillisecond);
        _defaultEasing = defaultEasing;
        _initialArcMode = initialArcMode;
        Delay = delay;
        Duration = duration;
    }

    public void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    )
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        ValueAtClamped(ClampPlayTime(playTime), initial, target, destination);
    }

    public void GetVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    )
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        var clamped = ClampPlayTime(playTime);

        EnsureInitialized(initial, target);

        if (_arcModes is not null)
        {
            var index = KeyframeMath.FindEntryIndex(_timestamps, clamped);
            _arcSpline!.GetSlope(GetEasedTime(index, clamped, asFraction: false), destination);
            return;
        }

        // Velocity is the derivative approximated by a 1ms finite difference.
        Span<double> now = stackalloc double[destination.Length];
        Span<double> before = stackalloc double[destination.Length];
        ValueAtClamped(clamped, initial, target, now);
        ValueAtClamped(Math.Max(0, clamped - 1), initial, target, before);
        for (var i = 0; i < destination.Length; i++)
            destination[i] = (now[i] - before[i]) * 1000.0;
    }

    private void ValueAtClamped(
        int clamped,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        Span<double> destination)
    {
        if (_keyframes.TryGetValue(clamped, out var keyframe))
        {
            keyframe.Value.CopyTo(destination);
            return;
        }

        if (clamped >= _durationMillis)
        {
            target.CopyTo(destination);
            return;
        }

        if (clamped <= 0)
        {
            initial.CopyTo(destination);
            return;
        }

        EnsureInitialized(initial, target);
        var index = KeyframeMath.FindEntryIndex(_timestamps, clamped);

        if (_arcModes is not null)
        {
            _arcSpline!.GetPosition(GetEasedTime(index, clamped, asFraction: false), destination);
            return;
        }

        var easedFraction = GetEasedTime(index, clamped, asFraction: true);
        var startValue = KeyframeValueAt(_timestamps[index], initial, target);
        var endValue = KeyframeValueAt(_timestamps[index + 1], initial, target);
        for (var i = 0; i < destination.Length; i++)
            destination[i] = startValue[i] + (endValue[i] - startValue[i]) * easedFraction;
    }

    /// <summary>Computes timestamps in seconds, detects arc modes and builds the arc spline.</summary>
    private void EnsureInitialized(ReadOnlySpan<double> initial, ReadOnlySpan<double> target)
    {
        if (_times is null)
        {
            _times = new double[_timestamps.Length];
            var modes = new int[_timestamps.Length];
            var requiresArcSpline = false;

            for (var i = 0; i < _timestamps.Length; i++)
            {
                _times[i] = _timestamps[i] / 1000.0;
                var mode = _keyframes.TryGetValue(_timestamps[i], out var info) ? info.ArcMode : _initialArcMode;
                modes[i] = (int)mode;
                if (mode != ArcMode.ArcLinear)
                    requiresArcSpline = true;
            }

            if (requiresArcSpline)
                _arcModes = modes;
        }

        if (_arcModes is null)
            return;

        // The spline start/end values follow initial/target when they are not defined as keyframes.
        if (_arcSpline is not null &&
            _lastInitial!.AsSpan().SequenceEqual(initial) &&
            _lastTarget!.AsSpan().SequenceEqual(target))
            return;

        var dimensionCount = initial.Length % 2 + initial.Length;
        var values = new double[_timestamps.Length][];
        for (var i = 0; i < _timestamps.Length; i++)
        {
            var row = new double[dimensionCount];
            KeyframeValueAt(_timestamps[i], initial, target).CopyTo(row);
            values[i] = row;
        }

        _lastInitial = initial.ToArray();
        _lastTarget = target.ToArray();
        _arcSpline = new ArcSpline(_arcModes!, _times!, values);
    }

    private ReadOnlySpan<double> KeyframeValueAt(int timestamp, ReadOnlySpan<double> initial, ReadOnlySpan<double> target)
    {
        if (timestamp == 0 && !_keyframes.ContainsKey(0))
            return initial;
        if (timestamp == _durationMillis && !_keyframes.ContainsKey(_durationMillis))
            return target;
        return _keyframes[timestamp].Value;
    }

    private int ClampPlayTime(TimeSpan playTime)
    {
        var playTimeMillis = playTime.Ticks / TimeSpan.TicksPerMillisecond;
        return (int)Math.Clamp(playTimeMillis - _delayMillis, 0L, _durationMillis);
    }

    /// <summary>
    /// Returns the eased time (in seconds when <paramref name="asFraction"/> is false) for the
    /// interval starting at <paramref name="index"/>.
    /// </summary>
    private double GetEasedTime(int index, int timeMillis, bool asFraction)
    {
        if (index >= _timestamps.Length - 1)
            return timeMillis / 1000.0;

        var timeMin = _timestamps[index];
        var timeMax = _timestamps[index + 1];

        if (timeMillis == timeMin)
            return timeMin / 1000.0;

        var timeRange = timeMax - timeMin;
        var easing = _keyframes.TryGetValue(timeMin, out var info) ? info.Easing : _defaultEasing;
        var rawFraction = (double)(timeMillis - timeMin) / timeRange;
        var easedFraction = easing.Ease(rawFraction);

        return asFraction
            ? easedFraction
            : (timeRange * easedFraction + timeMin) / 1000.0;
    }
}
