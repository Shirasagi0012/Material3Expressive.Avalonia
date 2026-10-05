using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;

/// <summary>
/// A keyframe-based animation that uses a monotone cubic Hermite spline to interpolate between
/// the values in all the intervals, producing a smooth, curvy motion.
/// </summary>
internal sealed class VectorizedMonoSplineKeyframesSpec : IVectorizedDurationBasedAnimationSpec
{
    private readonly int[] _timestamps;
    private readonly Dictionary<int, (double[] Value, Easing Easing)> _keyframes;
    private readonly int _durationMillis;
    private readonly int _delayMillis;
    private readonly double _periodicBias;

    // Lazily initialized on the first interpolation.
    private double[]? _times;
    private double[][]? _values;
    private MonoSpline? _monoSpline;
    private double[]? _lastInitial;
    private double[]? _lastTarget;

    public TimeSpan Delay { get; }
    public TimeSpan Duration { get; }

    internal VectorizedMonoSplineKeyframesSpec(
        IReadOnlyList<int> timestamps,
        IReadOnlyDictionary<int, (double[] Value, Easing Easing)> keyframes,
        TimeSpan duration,
        TimeSpan delay,
        double periodicBias)
    {
        ArgumentNullException.ThrowIfNull(timestamps);
        ArgumentNullException.ThrowIfNull(keyframes);
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        _timestamps = [.. timestamps];
        _keyframes = new Dictionary<int, (double[], Easing)>(keyframes);
        _durationMillis = (int)(duration.Ticks / TimeSpan.TicksPerMillisecond);
        _delayMillis = (int)(delay.Ticks / TimeSpan.TicksPerMillisecond);
        _periodicBias = periodicBias;
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
        var clamped = ClampPlayTime(playTime);

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
        _monoSpline!.GetPosition(GetEasedTime(index, clamped), destination, index);
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
        var index = KeyframeMath.FindEntryIndex(_timestamps, clamped);
        _monoSpline!.GetSlope(GetEasedTime(index, clamped), destination, index);
    }

    /// <summary>
    /// The spline is built lazily and rebuilt only when the initial/target values change, since
    /// only the start and end points depend on them.
    /// </summary>
    private void EnsureInitialized(ReadOnlySpan<double> initial, ReadOnlySpan<double> target)
    {
        if (_times is null)
        {
            _times = new double[_timestamps.Length];
            for (var i = 0; i < _timestamps.Length; i++)
                _times[i] = _timestamps[i] / 1000.0;
        }

        var monoSpline = _monoSpline;
        if (monoSpline is null ||
            !_lastInitial!.AsSpan().SequenceEqual(initial) ||
            !_lastTarget!.AsSpan().SequenceEqual(target))
        {
            var initialChanged = monoSpline is null || !_lastInitial!.AsSpan().SequenceEqual(initial);
            var targetChanged = monoSpline is null || !_lastTarget!.AsSpan().SequenceEqual(target);
            _lastInitial = initial.ToArray();
            _lastTarget = target.ToArray();

            var dimension = initial.Length;
            if (_values is null)
            {
                _values = new double[_timestamps.Length][];
                for (var i = 0; i < _timestamps.Length; i++)
                    _values[i] = CreateKeyframeRow(_timestamps[i], initial, target, dimension);
            }
            else
            {
                // Only the start and end rows may need to be replaced.
                if (initialChanged && !_keyframes.ContainsKey(0))
                    _values[Array.BinarySearch(_timestamps, 0)] = CreateKeyframeRow(0, initial, target, dimension);
                if (targetChanged && !_keyframes.ContainsKey(_durationMillis))
                    _values[Array.BinarySearch(_timestamps, _durationMillis)] = CreateKeyframeRow(_durationMillis, initial, target, dimension);
            }

            _monoSpline = new MonoSpline(_times!, _values!, _periodicBias);
        }
    }

    private double[] CreateKeyframeRow(int timestamp, ReadOnlySpan<double> initial, ReadOnlySpan<double> target, int dimension)
    {
        var row = new double[dimension];
        if (timestamp == 0 && !_keyframes.ContainsKey(0))
            initial.CopyTo(row);
        else if (timestamp == _durationMillis && !_keyframes.ContainsKey(_durationMillis))
            target.CopyTo(row);
        else
            _keyframes[timestamp].Value.CopyTo(row);
        return row;
    }

    private int ClampPlayTime(TimeSpan playTime)
    {
        var playTimeMillis = playTime.Ticks / TimeSpan.TicksPerMillisecond;
        return (int)Math.Clamp(playTimeMillis - _delayMillis, 0L, _durationMillis);
    }

    /// <summary>Returns the eased time in seconds for the interval starting at <paramref name="index"/>.</summary>
    private double GetEasedTime(int index, int timeMillis)
    {
        if (index >= _timestamps.Length - 1)
            return timeMillis / 1000.0;

        var timeMin = _timestamps[index];
        var timeMax = _timestamps[index + 1];

        if (timeMillis == timeMin)
            return timeMin / 1000.0;

        var timeRange = timeMax - timeMin;
        var easing = _keyframes.TryGetValue(timeMin, out var keyframe) ? keyframe.Easing : ComposeEasings.Linear;
        var rawFraction = (double)(timeMillis - timeMin) / timeRange;
        var easedFraction = easing.Ease(rawFraction);

        return (timeRange * easedFraction + timeMin) / 1000.0;
    }
}
