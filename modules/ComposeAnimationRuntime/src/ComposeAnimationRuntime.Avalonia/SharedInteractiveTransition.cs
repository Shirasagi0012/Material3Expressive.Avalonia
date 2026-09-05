using Avalonia;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class SharedInteractiveTransition : IDisposable
{
    private readonly InteractiveTransition _progress;
    private readonly List<Track> _tracks = [];
    private bool _settled;

    public SharedInteractiveTransition(TimeSpan settleDuration = default)
        => _progress = new InteractiveTransition(settleDuration == default ? TimeSpan.FromMilliseconds(220) : settleDuration);

    public double Progress => _progress.Progress;
    public bool IsRunning => _progress.IsRunning;
    public bool IsSettled => _settled;
    public event Action<double>? ProgressChanged;
    public event Action<bool>? Settled;

    public void Add(SharedBounds target, Rect from, Rect to)
    {
        ArgumentNullException.ThrowIfNull(target);
        _tracks.Add(new Track(() => target.SnapTo(Interpolate(from, to, Progress))));
        Apply();
    }

    public void Add(SharedElement target, Rect from, Rect to)
    {
        ArgumentNullException.ThrowIfNull(target);
        _tracks.Add(new Track(() => target.SnapTo(Interpolate(from, to, Progress))));
        Apply();
    }

    public void SetProgress(double value)
    {
        if (_settled) return;
        _progress.SetProgress(value);
        Apply();
    }

    public void Release(double velocity = 0, double threshold = 0.5)
    {
        if (_settled) return;
        _progress.Release(velocity, threshold);
    }
    public bool Tick(TimeSpan delta)
    {
        var running = _progress.Tick(delta);
        Apply();
        if (!running) Finish(Progress >= 0.999);
        return running;
    }

    public void Cancel() { if (_settled) return; _progress.Cancel(); Apply(); Finish(false); }
    public void Complete() { if (_settled) return; _progress.Complete(); Apply(); Finish(true); }

    private void Finish(bool completed)
    {
        if (_settled) return;
        _settled = true;
        Settled?.Invoke(completed);
        _tracks.Clear();
    }

    public void Dispose()
    {
        if (!_settled)
            Cancel();
        _tracks.Clear();
        ProgressChanged = null;
        Settled = null;
    }

    private void Apply()
    {
        foreach (var track in _tracks) track.Apply();
        ProgressChanged?.Invoke(Progress);
    }

    private static Rect Interpolate(Rect from, Rect to, double progress)
        => new(
            from.X + (to.X - from.X) * progress,
            from.Y + (to.Y - from.Y) * progress,
            from.Width + (to.Width - from.Width) * progress,
            from.Height + (to.Height - from.Height) * progress);

    private sealed record Track(Action Apply);
}
