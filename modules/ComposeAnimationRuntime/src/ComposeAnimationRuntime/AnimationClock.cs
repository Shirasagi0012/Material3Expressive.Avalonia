namespace ComposeAnimationRuntime;

public interface IAnimationClock
{
    TimeSpan Time { get; }
    IDisposable Register(Func<TimeSpan, bool> tick, Func<bool> isRunning);
}

public interface IControllableAnimationClock : IAnimationClock
{
    bool AdvanceBy(TimeSpan delta);
    void Settle();
    void Reset();
}

/// <summary>Deterministic clock for driving a group of animations.</summary>
public sealed class ManualAnimationClock : IControllableAnimationClock
{
    private readonly List<Track> _tracks = [];

    public TimeSpan Time { get; private set; }

    public void Register<T>(Animatable<T> animation)
    {
        ArgumentNullException.ThrowIfNull(animation);
        Register(animation.Tick, () => animation.IsRunning);
    }

    public IDisposable Register(Func<TimeSpan, bool> tick, Func<bool> isRunning)
    {
        ArgumentNullException.ThrowIfNull(tick);
        ArgumentNullException.ThrowIfNull(isRunning);
        var track = new Track(tick, isRunning);
        _tracks.Add(track);
        return new DelegateDisposable(() => _tracks.Remove(track));
    }

    public bool AdvanceBy(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta));

        Time += delta;
        var running = false;
        for (var index = _tracks.Count - 1; index >= 0; index--)
        {
            var track = _tracks[index];
            if (!track.Tick(delta) && !track.IsRunning() && _tracks.Contains(track))
                _tracks.Remove(track);
            else
                running = true;
        }

        return running;
    }

    public void Settle()
    {
        for (var frame = 0; frame < 10_000 && _tracks.Count > 0; frame++)
        {
            if (!AdvanceBy(TimeSpan.FromMilliseconds(16)))
                break;
        }

        _tracks.Clear();
    }

    public void Reset()
    {
        _tracks.Clear();
        Time = TimeSpan.Zero;
    }

    private sealed record Track(Func<TimeSpan, bool> Tick, Func<bool> IsRunning);
    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
