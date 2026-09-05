using Avalonia.Controls;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class LayoutTargetAnimation<T> : IDisposable
{
    private readonly Control _control;
    private readonly DeferredTargetAnimation<T> _animation;
    private readonly IEqualityComparer<T> _comparer;
    private bool _disposed;

    public LayoutTargetAnimation(Control control, Animatable<T> value, IAnimationSpec<T> spec, IEqualityComparer<T>? comparer = null)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
        _animation = new DeferredTargetAnimation<T>(value, spec);
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _control.LayoutUpdated += OnLayoutUpdated;
    }

    public bool IsResolved => _animation.IsResolved;
    public bool IsRunning => _animation.IsRunning;
    public bool RetargetOnLayout { get; set; }
    public T Value => _animation.Value;

    public void SetTarget(Func<T> resolver)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _targetResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _animation.SetTarget(resolver);
    }

    public bool ResolveNow() => _animation.Resolve();
    public bool Tick(TimeSpan delta) => _animation.Tick(delta);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _control.LayoutUpdated -= OnLayoutUpdated;
        _animation.Cancel();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_animation.IsResolved && !RetargetOnLayout)
            return;
        if (_animation.IsResolved && _animation.Target is { } target)
        {
            var next = _targetResolver!();
            if (_comparer.Equals(target, next)) return;
            _animation.SetTarget(_targetResolver);
        }
        if (!_animation.IsResolved)
            _animation.Resolve();
    }

    private Func<T>? _targetResolver;
}
