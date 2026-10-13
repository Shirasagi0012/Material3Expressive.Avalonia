using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Retained, target-driven animation state. Updates are coalesced before the next animation
/// sample batch, including updates made during measure. A Visual owner reconnects across
/// scope rebuilds; a supplied scope owns the fixed connection's lifetime.
/// </summary>
public sealed class AnimatedState<T> : IAnimationValue<T>, IDisposable, IAnimatedStateCommit
{
    private readonly Visual? _owner;
    private readonly bool _fixedScope;
    private readonly ITwoWayConverter<T> _converter;
    private readonly Optional<T> _threshold;
    private readonly IAnimationSpec<T> _defaultSpec;
    private readonly AnimationVector _vectorScratch;
    private readonly List<AnimatedStateConnection<T>> _connections = [];

    // Host connection. _scope is the live scope; _endingScope remembers the scope that just
    // ended so its matching ScopeChanged can be recognised. _scopeResource releases this
    // state's registration with that scope.
    private Animatable<T> _animation;
    private AnimationScope? _scope;
    private AnimationScope? _endingScope;
    private IDisposable? _scopeResource;
    private AnimatedStateCoordinator? _coordinator;
    private FrameClock? _resumeClock;
    private AnimationState<T>? _resumeState;

    // Requested configuration (latest UpdateTarget) versus the configuration a run started with.
    private IAnimationSpec<T> _requestedSpec;
    private IAnimationSpec<T>? _runSpec;
    private IAnimationSpec<T>? _failedSpec;

    // Value and target state, plus the identities of the current connection and run.
    private T _value;
    private T _target;
    private T _publishedTarget;
    private T _runTarget = default!;
    private int _connectionGeneration;
    private int _runGeneration;

    private bool _attached;
    private bool _running;
    private bool _publishedRunning;
    private bool _failed;
    private bool _disposed;

    // ---- Construction --------------------------------------------------------------------------

    /// <summary>
    /// Connects to <paramref name="owner"/>'s scope and follows it across scope rebuilds. The
    /// state may be created before the owner is attached.
    /// </summary>
    public AnimatedState(Visual owner, T initialValue, ITwoWayConverter<T> converter,
        IAnimationSpec<T>? defaultAnimationSpec = null, Optional<T> visibilityThreshold = default,
        string? label = null)
        : this(initialValue, converter, defaultAnimationSpec, visibilityThreshold, label)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
        _attached = TopLevel.GetTopLevel(owner) is not null;
        owner.AttachedToVisualTree += OnAttached;
        owner.DetachedFromVisualTree += OnDetached;
        AnimationScope.AddScopeChangedHandler(owner, OnScopeChanged);
        try
        {
            if (_attached) Connect(AnimationScope.Get(owner));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Connects to a fixed <paramref name="scope"/>. The scope owns the connection: ending it
    /// disposes this state.
    /// </summary>
    public AnimatedState(AnimationScope scope, T initialValue, ITwoWayConverter<T> converter,
        IAnimationSpec<T>? defaultAnimationSpec = null, Optional<T> visibilityThreshold = default,
        string? label = null)
        : this(initialValue, converter, defaultAnimationSpec, visibilityThreshold, label)
    {
        ArgumentNullException.ThrowIfNull(scope);
        scope.VerifyActive();
        _fixedScope = _attached = true;
        Connect(scope);
    }

    private AnimatedState(T initialValue, ITwoWayConverter<T> converter,
        IAnimationSpec<T>? defaultSpec, Optional<T> threshold, string? label)
    {
        Dispatcher.UIThread.VerifyAccess();
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentOutOfRangeException.ThrowIfLessThan(converter.Dimension, 1);
        _converter = converter;
        _threshold = threshold;
        _vectorScratch = new AnimationVector(converter.Dimension);
        Validate(initialValue);
        Label = label ?? "AnimatedState";
        _value = _target = _publishedTarget = initialValue;
        _defaultSpec = defaultSpec ?? new SpringSpec<T>(visibilityThreshold: VisibilityThresholds.For(converter));
        _requestedSpec = _defaultSpec;
        _animation = new Animatable<T>(converter, initialValue, label: Label);
    }

    // ---- Public surface ------------------------------------------------------------------------

    public T Value { get { VerifyAccess(); return _value; } }
    public T TargetValue { get { VerifyAccess(); return _target; } }
    public bool IsRunning { get { VerifyAccess(); return _running; } }
    public string Label { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<T>? ValueChanged;
    public event Action<T>? Finished;

    /// <summary>Records a target without evaluating a spec, publishing a value, or starting a run.</summary>
    public void UpdateTarget(T targetValue, IAnimationSpec<T>? animationSpec = null)
    {
        VerifyActive();
        var spec = animationSpec ?? _defaultSpec;
        // A new target or a different spec instance clears a previous failure so it can retry.
        if (!Equal(_target, targetValue) || !ReferenceEquals(_failedSpec, spec)) _failed = false;
        _target = targetValue;
        _requestedSpec = spec;
        Schedule();
    }

    /// <summary>
    /// Connects an output at Animation priority. Dispose the returned handle when replacing
    /// a template part. A detached target suspends only its contribution, not this animation.
    /// </summary>
    public IDisposable Bind(Visual target, StyledProperty<T> property)
    {
        VerifyActive();
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        var connection = new AnimatedStateConnection<T>(this, target, property);
        _connections.Add(connection);
        connection.Activate();
        return connection;
    }

    public void Dispose()
    {
        VerifyAccess();
        if (_disposed) return;
        _disposed = true;
        _connectionGeneration++;
        _runGeneration++;
        _coordinator?.Remove(this);
        foreach (var connection in _connections.ToArray()) connection.Dispose();
        if (_owner is not null)
        {
            _owner.AttachedToVisualTree -= OnAttached;
            _owner.DetachedFromVisualTree -= OnDetached;
            AnimationScope.RemoveScopeChangedHandler(_owner, OnScopeChanged);
        }
        _scopeResource?.Dispose();
        _scopeResource = null;
        _scope = _endingScope = null;
        _coordinator = null;
        _resumeClock = null;
        _resumeState = null;
        _running = false;
        _animation.Stop();
        PropertyChanged = null;
        ValueChanged = Finished = null;
    }

    // ---- Internal surface ----------------------------------------------------------------------

    internal AnimationScope? ActiveScope => !_disposed && _attached && _scope is { IsEnding: false } ? _scope : null;
    internal void Remove(AnimatedStateConnection<T> connection) => _connections.Remove(connection);
    internal void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();

    private void VerifyActive()
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static bool Equal(T left, T right) => EqualityComparer<T>.Default.Equals(left, right);

    // ---- Owner attachment and scope lifecycle --------------------------------------------------

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_disposed) return;
        _attached = true;
        if (!AnimationScope.IsEnvironmentEnding(_owner!)) Connect(AnimationScope.Get(_owner!));
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        _coordinator?.Remove(this);
        SuspendConnections();
    }

    private void SuspendConnections()
    {
        foreach (var connection in _connections.ToArray()) connection.Suspend();
    }

    private void OnScopeChanged(object? sender, AnimationScopeChangedEventArgs e)
    {
        if (_disposed || !ReferenceEquals(e.OldScope, _endingScope ?? _scope)) return;
        _endingScope = null;
        if (e.NewScope is { } scope) Connect(scope);
    }

    private void Connect(AnimationScope scope)
    {
        if (_disposed || scope.IsEnding) return;

        if (!ReferenceEquals(_scope, scope))
        {
            _scopeResource?.Dispose();
            _scope = scope;
            _connectionGeneration++;
            _failed = false;
            _coordinator = AnimatedStateCoordinator.Get(scope);
            _scopeResource = scope.RegisterResource(() => { if (scope.IsEnding) OnScopeEnding(scope); });
            // Old runs may still be unwinding. A fresh private runtime object avoids their
            // finally blocks clearing the replacement's restored velocity/timebase.
            _animation = new Animatable<T>(_converter, _value, label: Label);
            if (_resumeState is { } snapshot)
            {
                // A different root has a different time origin; drop the old frame timebase.
                if (!ReferenceEquals(_resumeClock, scope.FrameClock))
                    snapshot.LastFrameTime = AnimationConstants.UnspecifiedTime;
                _animation.ResumeFrom(snapshot);
            }
        }

        foreach (var connection in _connections.ToArray())
        {
            if (ActiveScope is null) break;
            connection.Activate();
        }

        Schedule();
    }

    private void OnScopeEnding(AnimationScope scope)
    {
        if (_disposed || !ReferenceEquals(scope, _scope)) return;

        _resumeState = _running ? _animation.State : null;
        _resumeClock = scope.FrameClock;
        _endingScope = scope;
        _scope = null;
        _scopeResource = null;
        _connectionGeneration++;
        _runGeneration++;
        _coordinator?.Remove(this);
        _coordinator = null;
        SuspendConnections();

        if (_fixedScope)
        {
            Dispose();
            return;
        }

        // A scope that ended while detached (or with its environment) freezes the value: the
        // next attach restarts from that value with zero velocity and a fresh timebase.
        if (!_attached || AnimationScope.IsEnvironmentEnding(_owner!))
        {
            _resumeState = null;
            _resumeClock = null;
            _running = false;
        }
    }

    // ---- Commit scheduling ---------------------------------------------------------------------

    private void Schedule()
    {
        if (_failed || ActiveScope is null || _coordinator is null) return;

        var needsRun = _resumeState is not null ||
            (_running ? !Equal(_target, _runTarget) : !Equal(_value, _target));
        var needsNotification = !Equal(_publishedTarget, _target) || _running != _publishedRunning;

        if (needsRun || needsNotification) _coordinator.Schedule(this);
        else _coordinator.Remove(this);
    }

    Action IAnimatedStateCommit.CaptureCommit()
    {
        var target = _target;
        var spec = _requestedSpec;
        var generation = _connectionGeneration;
        return () => Commit(target, spec, generation);
    }

    void IAnimatedStateCommit.RejectCommitFrame(Exception error, bool report)
    {
        if (ActiveScope is not { } scope) return;
        _failed = true;
        _failedSpec = _requestedSpec;
        _resumeState = null;
        _runGeneration++;
        _animation.Stop();
        try { SetRunning(false); }
        catch (Exception notificationError) { scope.ReportFailure(notificationError, Label); }
        if (report) scope.ReportFailure(error, Label);
    }

    private void Commit(T snapshotTarget, IAnimationSpec<T> snapshotSpec, int generation)
    {
        if (generation != _connectionGeneration || ActiveScope is not { } scope) return;

        bool IsCurrent() => generation == _connectionGeneration && ActiveScope is not null;

        try
        {
            if (!Equal(_publishedTarget, _target))
            {
                _publishedTarget = _target;
                NotifyProperty(nameof(TargetValue));
                if (!IsCurrent()) return;
            }
            if (_failed) return;

            var resume = _resumeState;
            if (resume is null && (_running ? Equal(snapshotTarget, _runTarget) : Equal(snapshotTarget, _value)))
            {
                // Already at the target: publish any pending running change without a new run.
                SetRunning(_running);
                return;
            }

            Validate(snapshotTarget);
            if (!IsCurrent()) return;

            // Resuming toward the same run target keeps that run's spec; anything else snapshots
            // the requested spec so later mutation of the spec object cannot change this run.
            var runSpec = resume is not null && Equal(snapshotTarget, _runTarget) && _runSpec is not null
                ? _runSpec
                : SnapshotSpec(snapshotSpec);
            if (!IsCurrent()) return;

            _resumeState = null;
            _runTarget = snapshotTarget;
            _runSpec = runSpec;
            var run = ++_runGeneration;
            SetRunning(true);
            if (!IsCurrent() || run != _runGeneration) return;

            var animation = _animation;
            _ = scope.Launch(_ => DriveAsync(scope, animation, snapshotTarget, runSpec, snapshotSpec, run, generation), Label);
        }
        catch (Exception error)
        {
            Fail(scope, snapshotTarget, snapshotSpec, error);
        }
    }

    // ---- Running the animation -----------------------------------------------------------------

    private async Task DriveAsync(AnimationScope scope, Animatable<T> animation, T runTarget,
        IAnimationSpec<T> runSpec, IAnimationSpec<T> requestedSpec, int run, int generation)
    {
        bool IsCurrent() => !_disposed && run == _runGeneration && generation == _connectionGeneration
            && ReferenceEquals(scope, ActiveScope);

        Task<AnimationResult<T>>? task = null;
        try
        {
            task = animation.AnimateToAsync(scope, runTarget, runSpec, block: track =>
            {
                if (!IsCurrent()) return;
                try
                {
                    Validate(track.Value);
                    if (Equal(_value, track.Value)) return;
                    _value = track.Value;
                    foreach (var connection in _connections.ToArray())
                    {
                        if (!IsCurrent()) return;
                        connection.Publish(_value);
                    }
                    if (!IsCurrent()) return;
                    NotifyProperty(nameof(Value));
                    if (IsCurrent()) Notify(ValueChanged, _value);
                }
                catch (Exception error)
                {
                    if (IsCurrent()) Fail(scope, runTarget, requestedSpec, error);
                }
            });

            var result = await task;
            if (!IsCurrent()) return;
            SetRunning(false);
            if (IsCurrent() && result.EndReason == AnimationEndReason.Finished && Equal(_target, runTarget))
            {
                // Finished is user code, but its failure cannot turn a successful motion into
                // a failed request that will be retried from layout.
                try { Notify(Finished, _value); }
                catch (Exception error) { scope.ReportFailure(error, Label); }
            }
        }
        catch (OperationCanceledException) when (task?.IsCanceled == true)
        {
            if (IsCurrent())
            {
                _failed = true;
                _failedSpec = requestedSpec;
                SetRunning(false);
            }
            throw; // Launch observes cancellation; do not turn canceled work into success.
        }
        catch (Exception error)
        {
            if (IsCurrent()) Fail(scope, runTarget, requestedSpec, error);
            else throw; // Cleanup faults remain observable even after disposal or replacement.
        }
    }

    // ---- Notifications -------------------------------------------------------------------------

    private void SetRunning(bool value)
    {
        _running = value;
        if (_publishedRunning == value) return;
        _publishedRunning = value;
        NotifyProperty(nameof(IsRunning));
    }

    private void NotifyProperty(string name)
    {
        var handlers = PropertyChanged;
        if (handlers is null) return;

        // Each handler can dispose or reconnect the state; stop as soon as that happens.
        var generation = _connectionGeneration;
        var args = new PropertyChangedEventArgs(name);
        foreach (PropertyChangedEventHandler handler in handlers.GetInvocationList())
        {
            if (_disposed || ActiveScope is null || generation != _connectionGeneration) return;
            handler(this, args);
        }
    }

    private void Notify(Action<T>? handlers, T value)
    {
        if (handlers is null) return;

        // Each handler can dispose or reconnect the state; stop as soon as that happens.
        var generation = _connectionGeneration;
        foreach (Action<T> handler in handlers.GetInvocationList())
        {
            if (_disposed || ActiveScope is null || generation != _connectionGeneration) return;
            handler(value);
        }
    }

    // ---- Failure and validation -----------------------------------------------------------------

    private void Fail(AnimationScope scope, T target, IAnimationSpec<T> spec, Exception error)
    {
        // Only remember the failure while it still describes the latest request.
        _failed = Equal(_target, target) && ReferenceEquals(_requestedSpec, spec);
        _failedSpec = spec;
        _resumeState = null;
        _runGeneration++;
        _animation.Stop();
        try { SetRunning(false); }
        catch (Exception notificationError) { scope.ReportFailure(notificationError, Label); }
        scope.ReportFailure(error, Label);
        if (!_disposed) Schedule();
    }

    private void Validate(T value)
    {
        _converter.To(value, _vectorScratch.AsSpan());
        foreach (var component in _vectorScratch.AsReadOnlySpan())
        {
            if (!double.IsFinite(component))
                throw new ArgumentException("AnimatedState requires finite animation values.");
        }
    }

    private IAnimationSpec<T> SnapshotSpec(IAnimationSpec<T> spec)
    {
        IVectorizedAnimationSpec vectorized;
        if (_threshold.HasValue && spec is SpringSpec<T> spring)
        {
            // Apply the explicit threshold override while keeping the spring's damping/stiffness.
            var threshold = new double[_converter.Dimension];
            _converter.To(_threshold.Value, threshold);
            vectorized = new VectorizedSpringSpec(spring.DampingRatio, spring.Stiffness, threshold, threshold.Length);
        }
        else
        {
            vectorized = spec.Vectorize(_converter);
        }
        return new SnapshotAnimationSpec(vectorized);
    }

    private sealed class SnapshotAnimationSpec(IVectorizedAnimationSpec spec) : IAnimationSpec<T>
    {
        public IVectorizedAnimationSpec Vectorize(ITwoWayConverter<T> converter) => spec;
    }
}
