namespace ComposeAnimationRuntime;

/// <summary>Deterministic clock for driving a group of animations.</summary>
public sealed class ManualAnimationClock
{
    private readonly List<Func<TimeSpan, bool>> _tracks = [];

    public TimeSpan Time { get; private set; }

    public void Register<T>(Animatable<T> animation)
    {
        ArgumentNullException.ThrowIfNull(animation);
        _tracks.Add(animation.Tick);
    }

    public bool AdvanceBy(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta));

        Time += delta;
        var running = false;
        for (var index = _tracks.Count - 1; index >= 0; index--)
        {
            if (_tracks[index](delta))
                running = true;
            else
                _tracks.RemoveAt(index);
        }

        return running;
    }

    public void Settle()
    {
        _tracks.Clear();
    }

    public void Reset()
    {
        _tracks.Clear();
        Time = TimeSpan.Zero;
    }
}
