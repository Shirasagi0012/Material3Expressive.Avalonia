using Avalonia;
using Avalonia.Data;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal sealed class AnimatedPropertySession<T> : IPropertySession
{
    private readonly Visual _host;
    private readonly AnimatedProperty<T> _declaration;
    private readonly StyledProperty<T> _property;
    private readonly ITwoWayConverter<T> _converter;
    private readonly AnimationVector _scratch;
    private FrameScope? _runScope;
    private AnimationScope? _scope;
    private Animatable<T>? _animatable;
    private AnimationState<T>? _interrupted;
    private AnimationSampleObservable<T>? _observable;
    private IDisposable? _binding;
    private IDisposable? _invalidation;
    private T _displayed = default!;
    private int _driveGen;
    private int _snapErrorGeneration;
    private bool _enabled;
    private bool _disposed;

    public AnimatedPropertySession(Visual host, AnimatedProperty<T> declaration)
    {
        _host = host;
        _declaration = declaration;
        _converter = declaration.Converter;
        var property = declaration.Property;
        if (property?.IsDirect == true)
            throw new InvalidOperationException($"Cannot animate direct property {property}.");
        if (property is not StyledProperty<T> styled)
            throw new InvalidCastException($"Property '{property}' does not support animated {typeof(T).Name} values.");
        if (styled.Inherits)
            throw new NotSupportedException(
                $"Animated properties do not support inherited property {styled}: Avalonia does not expose its covered base value.");
        _property = styled;
        _scratch = new AnimationVector(_converter.Dimension);
    }

    public AvaloniaProperty Property => _property;

    public void Attach(FrameScope? runScope, AnimationScope? scope = null)
    {
        _invalidation?.Dispose();
        _invalidation = null;
        StopDrive();
        _interrupted = null;
        _runScope = runScope;
        _scope = scope;
        _enabled = runScope is not null;
        if (!_enabled) return;
        _invalidation = RegisterEnding(scope!);
        _displayed = _host.GetValue(_property);
        RecreateAnimatable();
    }

    public void Resume(FrameScope runScope, AnimationScope scope)
    {
        var animatable = _animatable;
        if (_disposed || animatable is null || !IsFinite(animatable.Value)) { Attach(runScope, scope); return; }
        _invalidation?.Dispose();
        _driveGen++;
        StopOverlay();
        _runScope = runScope;
        _scope = scope;
        _enabled = true;
        _invalidation = RegisterEnding(scope);
        _displayed = animatable.Value;
        if (_interrupted is { } interrupted) animatable.ResumeFrom(interrupted);
        _interrupted = null;
        // Keeps the Animatable (value and velocity; its old run was cancelled with the old
        // scope); ReconcileTarget re-runs it toward the base value in the new scope.
        ReconcileTarget();
    }

    // A resource callback also runs when the registration is disposed (Attach, Resume, Dispose);
    // only the scope's own end is an interruption to snapshot.
    private IDisposable RegisterEnding(AnimationScope scope)
        => scope.RegisterResource(() => { if (scope.IsEnding) OnScopeEnding(); });

    // Runs before the scope cancels its runs: snapshot the in-flight state so a D17 resume
    // continues like an in-place interruption (velocity and frame timebase).
    private void OnScopeEnding()
    {
        _interrupted = _animatable is { IsRunning: true } animatable ? animatable.State : null;
        _enabled = false;
        _driveGen++;
    }

    public void ReconcileTarget()
    {
        if (!_enabled || _disposed || _scope?.IsEnding == true) return;
        var target = ReadBase();
        var animatable = _animatable;
        // Match animate*AsState: retarget when the destination differs from the in-flight
        // Animatable target. Overlay-hidden base changes are detected on frames, not
        // PropertyChanged, because the effective value may not change.
        if (animatable is { IsRunning: true }
            && EqualityComparer<T>.Default.Equals(target, animatable.TargetValue))
            return;
        if (animatable is not { IsRunning: true }
            && EqualityComparer<T>.Default.Equals(target, _displayed))
            return;

        if (!IsFinite(_displayed) || !IsFinite(target))
        {
            StopDrive();
            _displayed = ReadBase();
            RecreateAnimatable();
            return;
        }

        if (_animatable is null)
            RecreateAnimatable();
        if (_animatable is null) return;

        EnsureOverlay(_displayed);
        if (!_enabled || _scope!.IsEnding) return;
        // AnimateToAsync must not be awaited on the frame-callback stack: MutatorMutex still
        // holds the section, so a nested wait would deadlock.
        _ = _scope!.Launch(_ => DriveAsync(target), $"Property {_property.Name}");
    }

    public void SnapToTarget()
    {
        _snapErrorGeneration = _driveGen;
        Abort();
    }

    public void Abort()
    {
        _displayed = ReadBase();
        StopDrive();
        RecreateAnimatable();
    }

    public void Dispose()
    {
        _disposed = true;
        _enabled = false;
        _invalidation?.Dispose();
        _invalidation = null;
        StopDrive();
        _runScope = null;
    }

    private async Task DriveAsync(T target)
    {
        var gen = ++_driveGen;
        var animatable = _animatable;
        var scope = _runScope;
        if (animatable is null || scope is null) return;
        try
        {
            await animatable.AnimateToAsync(scope, target, _declaration.ResolveSpec(), block: OnFrame)
                .ConfigureAwait(true);
            if (gen != _driveGen || _disposed || !_enabled) return;
            if (!EqualityComparer<T>.Default.Equals(ReadBase(), animatable.Value))
            {
                ReconcileTarget();
                return;
            }

            StopOverlay();
            _displayed = _host.GetValue(_property);
        }
        catch (OperationCanceledException)
        {
            // Keep the returned task canceled so sequential managed flows cannot continue.
            throw;
        }
        catch (Exception)
        {
            if (gen <= _snapErrorGeneration) return;
            if (gen == _driveGen && !_disposed) Abort();
            throw;
        }
    }

    private void OnFrame(Animatable<T> animatable)
    {
        if (_disposed || !_enabled || _scope?.IsEnding == true) return;
        _displayed = animatable.Value;
        _observable?.Publish(_displayed);
        ReconcileTarget();
    }

    private T ReadBase()
    {
        var value = _host.GetBaseValue(_property);
        return value.HasValue
            ? value.Value
            : _property.CoerceValue(_host, _property.GetDefaultValue(_host));
    }

    private bool IsFinite(T value)
    {
        _converter.To(value, _scratch.AsSpan());
        foreach (var component in _scratch.AsReadOnlySpan())
            if (!double.IsFinite(component)) return false;
        return true;
    }

    private void EnsureOverlay(T value)
    {
        if (_binding is not null) return;
        _observable = new AnimationSampleObservable<T>(value);
        var contribution = _host.Bind(_property, _observable, BindingPriority.Animation);
        if (_scope!.IsEnding) { contribution.Dispose(); return; }
        _binding = _scope.RegisterResource(contribution.Dispose, binding: true);
    }

    private void StopDrive()
    {
        _driveGen++;
        var animation = _animatable;
        _animatable = null;
        try { animation?.Stop(); }
        finally { StopOverlay(); }
    }

    private void RecreateAnimatable()
    {
        _animatable?.Stop();
        _animatable = null;
        if (!_enabled || _runScope is null || _disposed) return;
        if (!IsFinite(_displayed)) return;
        _animatable = new Animatable<T>(_converter, _displayed);
    }

    private void StopOverlay()
    {
        var binding = _binding;
        _binding = null;
        _observable = null;
        binding?.Dispose();
    }
}

internal sealed class AnimationSampleObservable<T>(T value) : IObservable<T>
{
    private IObserver<T>? _observer;
    private T _value = value;

    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observer = observer;
        observer.OnNext(_value);
        return new Subscription(this, observer);
    }

    public void Publish(T value)
    {
        _value = value;
        _observer?.OnNext(value);
    }

    private sealed class Subscription(AnimationSampleObservable<T> owner, IObserver<T> observer) : IDisposable
    {
        public void Dispose()
        {
            if (ReferenceEquals(owner._observer, observer))
                owner._observer = null;
        }
    }
}
