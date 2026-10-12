namespace ComposeAnimation.Runtime;

/// <summary>
/// The one play-time model of every run loop (Animatable/SuspendAnimation, Transition,
/// InfiniteTransition, seeks): each frame adds the frame delta divided by the effective duration
/// scale read at that frame, so a scale change applies from the next frame without a jump or a
/// restart. The fractional tick left by the division is carried to the next frame, so long runs
/// at a non-integer ratio do not drift. The first frame after a start or <see cref="Pause"/> adds
/// nothing: inactive time is not counted and a resumed run continues from <see cref="PlayTime"/>.
/// </summary>
internal struct PlayTimeAccumulator
{
    private TimeSpan _last;
    private bool _hasLast;
    private double _remainder;

    public TimeSpan PlayTime { get; private set; }

    /// <summary>True when <paramref name="frame"/> is earlier than the previous frame.</summary>
    public readonly bool IsBeforeLast(TimeSpan frame) => _hasLast && frame < _last;

    /// <summary>Adds this frame's delta at <paramref name="scale"/> (0 adds nothing) and returns the play time.</summary>
    public TimeSpan Advance(TimeSpan frame, double scale)
    {
        if (_hasLast && scale > 0)
        {
            var exact = Math.Max(0, (frame - _last).Ticks) / scale + _remainder;
            var whole = Math.Round(exact);
            _remainder = exact - whole;
            PlayTime += TimeSpan.FromTicks(checked((long)whole));
        }
        _last = frame;
        _hasLast = true;
        return PlayTime;
    }

    /// <summary>Keeps the play time; the next frame starts a new delta chain.</summary>
    public void Pause()
    {
        _hasLast = false;
        _remainder = 0;
    }

    /// <summary>Sets the play time (0 by default) and starts a new delta chain.</summary>
    public void Reset(TimeSpan playTime = default)
    {
        PlayTime = playTime;
        Pause();
    }
}
