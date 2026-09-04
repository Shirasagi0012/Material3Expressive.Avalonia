using Avalonia;
using Avalonia.Controls;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public static class MotionBinding
{
    public static IDisposable Bind<T>(AvaloniaObject owner, AvaloniaProperty<T> property, Animatable<T> animation)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(animation);
        owner.SetValue(property, animation.Value);
        void Apply(T value) => owner.SetValue(property, value);
        animation.ValueChanged += Apply;
        return new BindingDisposable(() => animation.ValueChanged -= Apply);
    }

    private sealed class BindingDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}

public sealed class AvaloniaMotionHost : IDisposable
{
    private readonly TopLevel _topLevel;
    private readonly List<MotionTrack> _tracks = [];
    private TimeSpan? _lastFrame;
    private bool _scheduled;
    private bool _disposed;

    public AvaloniaMotionHost(TopLevel topLevel) => _topLevel = topLevel ?? throw new ArgumentNullException(nameof(topLevel));

    public void Register<T>(Animatable<T> animation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(animation);
        _tracks.Add(new MotionTrack(animation.Tick, () => animation.IsRunning));
        RequestFrame();
    }

    public void RequestFrame()
    {
        if (_disposed || _scheduled || _tracks.Count == 0) return;
        _scheduled = true;
        _topLevel.RequestAnimationFrame(OnFrame);
    }

    public void Dispose() { _disposed = true; _tracks.Clear(); _scheduled = false; _lastFrame = null; }

    private void OnFrame(TimeSpan timestamp)
    {
        if (_disposed) return;
        _scheduled = false;
        var delta = _lastFrame is { } previous ? timestamp - previous : TimeSpan.Zero;
        _lastFrame = timestamp;
        foreach (var track in _tracks)
            track.Tick(delta);
        if (!_tracks.Any(track => track.IsRunning())) _lastFrame = null;
        else RequestFrame();
    }
}

internal sealed record MotionTrack(Func<TimeSpan, bool> Tick, Func<bool> IsRunning);
