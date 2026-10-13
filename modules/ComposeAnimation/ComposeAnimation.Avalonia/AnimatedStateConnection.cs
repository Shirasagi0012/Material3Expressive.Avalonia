using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace ComposeAnimation;

// Owns only an output contribution. Its source owns the sole animation and frame demand.
internal sealed class AnimatedStateConnection<T> : IDisposable
{
    private readonly AnimatedState<T> _source;
    private readonly Visual _target;
    private readonly StyledProperty<T> _property;
    private AnimationScope? _scope;
    private AnimationSampleObservable<T>? _samples;
    private IDisposable? _binding;
    private int _generation;
    private bool _attached;
    private bool _disposed;
    private bool _failed;

    public AnimatedStateConnection(AnimatedState<T> source, Visual target, StyledProperty<T> property)
    {
        _source = source;
        _target = target;
        _property = property;
        _attached = TopLevel.GetTopLevel(target) is not null;
        target.AttachedToVisualTree += OnAttached;
        target.DetachedFromVisualTree += OnDetached;
    }

    public void Activate()
    {
        if (_disposed || !_attached || _binding is not null || _source.ActiveScope is not { } scope) return;
        _failed = false;
        _scope = scope;
        var generation = ++_generation;
        // Install the disposable before the first publication can re-enter or throw.
        var samples = _samples = new AnimationSampleObservable<T>(_source.Value, publishOnSubscribe: false);
        IDisposable? contribution = null;
        try
        {
            contribution = _target.Bind(_property, samples, BindingPriority.Animation);
            if (_disposed || !_attached || generation != _generation || !ReferenceEquals(scope, _source.ActiveScope))
            {
                contribution.Dispose();
                return;
            }
            _binding = scope.RegisterResource(contribution.Dispose, binding: true);
            samples.Publish(_source.Value);
        }
        catch (Exception error)
        {
            try { contribution?.Dispose(); }
            catch (Exception cleanupError) { scope.ReportFailure(cleanupError, _source.Label); }
            Fail(scope, error);
        }
    }

    public void Publish(T value)
    {
        if (_disposed || !_attached || _failed || _binding is null || _scope is not { IsEnding: false } scope) return;
        try { _samples?.Publish(value); }
        catch (Exception error) { Fail(scope, error); }
    }

    public void Suspend()
    {
        _generation++;
        var binding = _binding;
        var scope = _scope;
        _binding = null;
        _samples = null;
        _scope = null;
        try { binding?.Dispose(); }
        catch (Exception error) { scope?.ReportFailure(error, _source.Label); }
    }

    private void Fail(AnimationScope scope, Exception error)
    {
        Suspend();
        _failed = true;
        scope.ReportFailure(error, _source.Label);
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _attached = true;
        Activate();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        Suspend();
    }

    public void Dispose()
    {
        _source.VerifyAccess();
        if (_disposed) return;
        _disposed = true;
        _target.AttachedToVisualTree -= OnAttached;
        _target.DetachedFromVisualTree -= OnDetached;
        Suspend();
        _source.Remove(this);
    }
}
