using Avalonia;
using Avalonia.Controls;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal abstract class MotionPageBase : UserControl, IDisposable
{
    private readonly List<IDisposable> _resources = [];
    private bool _attached;
    private bool _disposed;

    protected AvaloniaMotionHost Host { get; private set; } = null!;

    public void Attach(AvaloniaMotionHost host)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_attached)
            return;

        Host = host ?? throw new ArgumentNullException(nameof(host));
        _attached = true;
        OnAttached();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        OnDetached();
        foreach (var resource in _resources)
            resource.Dispose();
        _resources.Clear();
    }

    protected virtual void OnAttached()
    {
    }

    protected virtual void OnDetached()
    {
    }

    protected void Bind<T>(AvaloniaObject owner, AvaloniaProperty<T> property, Animatable<T> animation)
    {
        BindProperty(owner, property, animation);
        _resources.Add(Host.Register(animation));
    }

    protected void BindProperty<T>(AvaloniaObject owner, AvaloniaProperty<T> property, Animatable<T> animation)
    {
        _resources.Add(MotionBinding.Bind(owner, property, animation));
    }

    protected void RegisterTrack(Func<TimeSpan, bool> tick, Func<bool> isRunning)
        => _resources.Add(Host.RegisterTrack(tick, isRunning));

    protected void BindValue<T>(Animatable<T> animation, Action<T> apply)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(apply);
        apply(animation.Value);
        animation.ValueChanged += apply;
        _resources.Add(new DelegateDisposable(() => animation.ValueChanged -= apply));
        _resources.Add(Host.Register(animation));
    }

    protected void RequestFrame() => Host.RequestFrame();

    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
