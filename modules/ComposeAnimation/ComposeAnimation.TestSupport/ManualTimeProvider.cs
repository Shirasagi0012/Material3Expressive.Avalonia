namespace ComposeAnimation.TestSupport;

/// <summary>
/// Manually advanced coroutine time, independent of ManualAnimationFrameClock. Advance on the
/// owning thread; timer changes and cancellation are thread-safe. Timer callbacks run in due-time
/// order. This provider does not pump a UI dispatcher or send animation frames.
/// </summary>
public sealed class ManualTimeProvider(Action? verifyAccess = null) : TimeProvider
{
    readonly object gate = new();
    readonly HashSet<ManualTimer> timers = [];
    long ticks, sequence;
    bool advancing;
    public TimeSpan Elapsed { get { lock (gate) return TimeSpan.FromTicks(ticks); } }
    public int ActiveTimerCount { get { lock (gate) return timers.Count; } }
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() { lock (gate) return ticks; }
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch + Elapsed;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        lock (gate)
        {
            var timer = new ManualTimer(this, callback, state, sequence++);
            timer.Change(dueTime, period);
            return timer;
        }
    }

    public void AdvanceBy(TimeSpan delta)
    {
        verifyAccess?.Invoke();
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        long target;
        lock (gate)
        {
            if (advancing) throw new InvalidOperationException("Recursive time advancement is not supported.");
            target = checked(ticks + delta.Ticks);
            advancing = true;
        }
        try
        {
            while (true)
            {
                ManualTimer? next;
                lock (gate)
                {
                    next = timers.Where(timer => timer.Due <= target).OrderBy(timer => timer.Due).ThenBy(timer => timer.Sequence).FirstOrDefault();
                    if (next is null) { ticks = target; return; }
                    ticks = next.Due;
                    if (next.Period > TimeSpan.Zero) next.Due = checked(ticks + next.Period.Ticks);
                    else { next.Due = long.MaxValue; timers.Remove(next); }
                }
                next.Callback(next.State);
            }
        }
        finally { lock (gate) advancing = false; }
    }

    sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state, long sequence) : ITimer
    {
        internal TimerCallback Callback { get; } = callback;
        internal object? State { get; } = state;
        internal long Sequence { get; } = sequence;
        internal long Due { get; set; } = long.MaxValue;
        internal TimeSpan Period { get; private set; }
        bool disposed;
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Validate(dueTime); Validate(period);
            lock (owner.gate)
            {
                if (disposed) return false;
                Period = period;
                Due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : checked(owner.ticks + dueTime.Ticks);
                owner.timers.Remove(this);
                if (dueTime != Timeout.InfiniteTimeSpan) owner.timers.Add(this);
                return true;
            }
        }
        static void Validate(TimeSpan value)
        {
            if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) throw new ArgumentOutOfRangeException(nameof(value));
        }
        public void Dispose() { lock (owner.gate) { disposed = true; owner.timers.Remove(this); } }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
