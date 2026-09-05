namespace ComposeAnimationRuntime;

public sealed class DeferredTargetAnimation<T>
{
    private readonly Animatable<T> _animation;
    private readonly IAnimationSpec<T> _spec;
    private Func<T>? _resolver;
    private bool _resolved;

    public DeferredTargetAnimation(Animatable<T> animation, IAnimationSpec<T> spec)
    {
        _animation = animation ?? throw new ArgumentNullException(nameof(animation));
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
    }

    public bool IsResolved => _resolved;
    public bool IsRunning => _animation.IsRunning;
    public T Value => _animation.Value;
    public T? Target { get; private set; }
    public void SetTarget(Func<T> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        Target = default;
        _resolved = false;
    }

    public bool Resolve()
    {
        if (_resolver is null || _resolved) return false;
        Target = _resolver();
        _animation.AnimateTo(Target, _spec);
        _resolved = true;
        return true;
    }

    public void Cancel() => _animation.Stop();
    public bool Tick(TimeSpan delta) => _animation.Tick(delta);
}
