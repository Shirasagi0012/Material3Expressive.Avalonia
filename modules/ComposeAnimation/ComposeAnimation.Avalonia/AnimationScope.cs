using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

public sealed class AnimationFailureEventArgs : EventArgs
{
    internal AnimationFailureEventArgs(Guid id, AnimationScope source, string? label, Exception error, Visual? host = null)
        => (FailureId, SourceScope, Host, OperationLabel, Exception) = (id, source, host ?? source.Host, label, error);
    public Guid FailureId { get; }
    public AnimationScope SourceScope { get; }
    public Visual Host { get; }
    public string? OperationLabel { get; }
    public Exception Exception { get; }
}

/// <summary>Owns animation work, samples and children within a fixed TopLevel environment.</summary>
public sealed class AnimationScope : IDisposable, IAsyncDisposable
{
    private static readonly ConditionalWeakTable<TopLevel, AnimationScope> Roots = new();
    private static readonly ConditionalWeakTable<Visual, AnimationScope> HostScopes = new();
    private static readonly AsyncLocal<Operation?> CurrentOperation = new();
    private readonly AnimationScope? _parent;
    private readonly AnimationScope _root;
    private readonly FrameClock _frameClock;
    private readonly AnimationLifetime _lifecycle = new();
    private readonly HashSet<AnimationScope> _children = [];
    private readonly HashSet<Operation> _tasks = [];
    private readonly HashSet<ScopeResource> _subscriptions = [];
    private readonly HashSet<ScopeResource> _bindings = [];
    private readonly ConditionalWeakTable<Exception, Failure> _failures;
    private readonly TaskCompletionSource _ended = new();
    private readonly CancellationToken _token;
    private bool _retainsMoves;
    private bool _ending;
    private bool _cleanupStarted;
    private double? _durationScale;

    private AnimationScope(Visual host, AnimationScope? parent, IAnimationFrameClock? clock, string? label,
        TimeProvider? timeProvider = null)
    {
        Host = host;
        Label = label;
        _parent = parent;
        if (parent is null) _durationScale = MotionDurationScale.ScaleFactor;
        _root = parent?._root ?? this;
        TimeProvider = parent?.TimeProvider ?? timeProvider ?? System.TimeProvider.System;
        // D3: only the root creates the window's FrameClock; every descendant copies the reference.
        _frameClock = parent?._frameClock ?? new FrameClock(clock!,
            onSourceFailure: error => Report(error, "Frame source", null, frameSource: true));
        _failures = _root == this ? new() : _root._failures;
        _token = _lifecycle.Token;
        _lifecycle.UnmanagedFailure = error =>
        {
            if (CurrentOperation.Value?.Active != true) ReportFailure(error);
        };
        Clock = new ScopeClock(this);
        parent?._children.Add(this);
        host.PropertyChanged += OnHostPropertyChanged;
        if (parent is not null) host.DetachedFromVisualTree += OnDetached;
    }

    public Visual Host { get; }
    public string? Label { get; }
    public IAnimationFrameClock Clock { get; }
    /// <summary>The window's frame clock, shared by every scope of this environment.</summary>
    internal FrameClock FrameClock => _frameClock;
    /// <summary>Coroutine delay time source, inherited by children; independent of frame timestamps and DurationScale.</summary>
    public TimeProvider TimeProvider { get; }
    public CancellationToken Lifetime => _token;
    public Exception? LastError { get; private set; }
    /// <summary>Last exception thrown by a diagnostic subscriber. Never recursively reported.</summary>
    public Exception? LastDiagnosticError { get; private set; }
    public event EventHandler<AnimationFailureEventArgs>? AnimationFailed;
    public bool IsEnding => _ending;
    public int ActiveTaskCount => _tasks.Count;
    public int ActiveChildCount => _children.Count;
    public int SubscriptionCount => _subscriptions.Count;
    public int BindingCount => _bindings.Count;
    public int PendingAnimationCount => _lifecycle.PendingCount;

    /// <summary>Inherited playback duration multiplier. Raw frame timestamps are unchanged.</summary>
    public double DurationScale
    {
        get => _durationScale ?? _parent?.DurationScale ?? 1;
        set
        {
            VerifyActive();
            if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _durationScale = value;
            PlaybackChanged();
        }
    }

    public static AnimationScope Initialize(TopLevel host, IAnimationFrameClock? clock = null)
        => Initialize(host, clock, System.TimeProvider.System);

    /// <summary>Initializes an environment with an explicit ordinary-delay scheduler.</summary>
    public static AnimationScope Initialize(TopLevel host, IAnimationFrameClock? clock, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(timeProvider);
        Dispatcher.UIThread.VerifyAccess();
        if (Roots.TryGetValue(host, out _)) throw new InvalidOperationException("This TopLevel already has an animation environment.");
        clock ??= new AvaloniaAnimationFrameClock(host);
        clock.VerifyAccess();
        var root = new AnimationScope(host, null, clock, "Root", timeProvider);
        Roots.Add(host, root);
        host.Closed += root.OnClosed;
        return root;
    }

    public static AnimationScope Create(Visual owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        Dispatcher.UIThread.VerifyAccess();
        var host = TopLevel.GetTopLevel(owner) ?? throw new InvalidOperationException("Animation scopes require an attached Visual with a TopLevel.");
        var environment = Resolve(host);
        environment.VerifyActive();
        return new AnimationScope(owner, environment, null, owner.GetType().Name);
    }

    private static AnimationScope Resolve(TopLevel host)
    {
        if (Roots.TryGetValue(host, out var root))
        {
            if (!root.IsEnding || host is not PopupRoot) return root;
            Roots.Remove(host);
        }
        if (host is PopupRoot popup && popup.ParentTopLevel is { } parent)
        {
            var environment = Resolve(parent);
            environment.VerifyActive();
            root = new AnimationScope(host, environment, null, "Popup");
            Roots.Add(host, root);
            host.Closed += root.OnClosed;
            return root;
        }
        throw new InvalidOperationException("Call AnimationScope.Initialize(topLevel, clock) before loading or setting its content.");
    }

    public AnimationScope CreateChild(string? label = null)
    {
        VerifyActive();
        return new AnimationScope(Host, this, null, label);
    }

    internal static AnimationScope ForHost(Visual owner)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (HostScopes.TryGetValue(owner, out var existing) && !existing.IsEnding) return existing;
        HostScopes.Remove(owner);
        var scope = Create(owner);
        scope._retainsMoves = owner is AnimatedBounds;
        HostScopes.Add(owner, scope);
        return scope;
    }

    internal static bool IsEnvironmentEnding(Visual owner)
        => TopLevel.GetTopLevel(owner) is { } host && Roots.TryGetValue(host, out var scope) && scope._root.IsEnding;

    internal static void ReportError(Visual host, Exception error)
    {
        if (HostScopes.TryGetValue(host, out var scope)) scope.Report(error, null, CurrentOperation.Value);
        else if (TopLevel.GetTopLevel(host) is { } top) Resolve(top).Report(error, null, CurrentOperation.Value, host);
    }

    internal void VerifyActive()
    {
        Dispatcher.UIThread.VerifyAccess();
        ObjectDisposedException.ThrowIf(_ending, this);
    }

    public Task Launch(Func<CancellationToken, Task> operation, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        VerifyActive();
        var entry = new Operation(this, CurrentOperation.Value, label);
        _tasks.Add(entry); // Before user code: covers synchronous reentry and disposal.
        var previous = CurrentOperation.Value;
        CurrentOperation.Value = entry;
        Task task;
        try { task = operation(Lifetime) ?? Task.FromException(new InvalidOperationException("An animation operation returned null.")); }
        catch (Exception error) { task = Task.FromException(error); }
        finally { CurrentOperation.Value = previous; }
        entry.Task = task;
        _ = ObserveAsync(entry);
        return task;
    }

    /// <summary>
    /// Suspends ordinary coroutine work without requesting animation frames or applying DurationScale.
    /// Cancellation follows both this scope and the caller. Completion resumes on the caller's context.
    /// </summary>
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return Launch(async lifetime =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellationToken);
            await Task.Delay(delay, TimeProvider, linked.Token).ConfigureAwait(true);
            // Match cancellable suspension: cancellation while continuation was queued wins.
            linked.Token.ThrowIfCancellationRequested();
        }, "Coroutine delay");
    }

    private async Task ObserveAsync(Operation entry)
    {
        try { await entry.Task.ConfigureAwait(true); }
        catch
        {
            // Task state, rather than exception type, distinguishes canceled work from faults.
            if (entry.Task.IsFaulted)
                foreach (var error in entry.Task.Exception!.InnerExceptions) Report(error, entry.Label, entry);
        }
        finally
        {
            entry.Active = false;
            _tasks.Remove(entry);
            entry.Completion.TrySetResult();
        }
    }

    internal void ReportFailure(Exception error, string? label = null) => Report(error, label, CurrentOperation.Value);

    private void Report(Exception error, string? label, Operation? operation, Visual? host = null, bool frameSource = false)
    {
        LastError = error;
        _root.LastError = error;
        foreach (var leaf in Leaves(error))
        {
            if (FindFailure(leaf, operation) is not null) continue;
            var ancestors = new HashSet<Guid>();
            for (var source = operation; source is not null; source = source.Parent) ancestors.Add(source.Id);
            var failure = new Failure(Guid.NewGuid(), ancestors, frameSource, leaf.StackTrace);
            _failures.Remove(leaf);
            _failures.Add(leaf, failure);
            var args = new AnimationFailureEventArgs(failure.Id, this, label, leaf, host);
            if (_root.AnimationFailed is { } handlers)
                foreach (EventHandler<AnimationFailureEventArgs> handler in handlers.GetInvocationList())
                    try { handler(_root, args); }
                    catch (Exception diagnosticError) { _root.LastDiagnosticError = diagnosticError; }
        }
    }

    private Failure? FindFailure(Exception error, Operation? operation)
    {
        if (_failures.TryGetValue(error, out var failure))
        {
            // Retain identity only through an actual managed operation chain; unrelated throws
            // of a reused exception are independent failures.
            if (failure.FrameSource || IsAwaitPropagation(error, failure.Trace) ||
                operation is not null && failure.Ancestors.Contains(operation.Id)) return failure;
        }
        return error.InnerException is { } inner ? FindFailure(inner, operation) : null;
    }

    private static bool IsAwaitPropagation(Exception error, string? origin)
    {
        // Await/EDI propagation preserves the previously thrown stack and extends it.
        // A fresh throw of a reused exception replaces that stack and remains independent.
        return !string.IsNullOrEmpty(origin) && error.StackTrace is { } trace &&
               trace.Length > origin.Length && trace.StartsWith(origin, StringComparison.Ordinal);
    }

    private static IEnumerable<Exception> Leaves(Exception error)
        => error is AggregateException { InnerExceptions.Count: > 0 } aggregate
            ? aggregate.InnerExceptions.SelectMany(Leaves) : [error];

    internal IDisposable RegisterResource(Action dispose, bool binding = false)
    {
        VerifyActive();
        var resource = new ScopeResource(this, dispose, binding);
        (binding ? _bindings : _subscriptions).Add(resource);
        return resource;
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_retainsMoves && Host is AnimatedBounds { RetainsAnimationAttachment: true }) return;
        Dispose();
    }
    private void OnClosed(object? sender, EventArgs e) => Dispose();
    private void OnHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Animated.DurationScaleProperty) PlaybackChanged();
    }
    private void PlaybackChanged()
    {
        _lifecycle.PlaybackChanged();
        foreach (var child in _children.ToArray()) child.PlaybackChanged();
    }

    public void Dispose()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (_cleanupStarted) return;
        MarkEnding();
        DisableConsumers(false);
        DisableConsumers(true);
        StartCleanup();
    }

    private void MarkEnding()
    {
        _ending = true;
        _lifecycle.MarkEnding();
        foreach (var child in _children.ToArray()) child.MarkEnding();
    }

    private void DisableConsumers(bool binding)
    {
        foreach (var child in _children.ToArray()) child.DisableConsumers(binding);
        foreach (var resource in (binding ? _bindings : _subscriptions).ToArray()) TryCleanup(resource.Dispose);
    }

    private void StartCleanup()
    {
        if (_cleanupStarted) return;
        _cleanupStarted = true;
        Host.DetachedFromVisualTree -= OnDetached;
        Host.PropertyChanged -= OnHostPropertyChanged;
        if (Host is TopLevel top) top.Closed -= OnClosed;
        foreach (var child in _children.ToArray()) child.StartCleanup();
        TryCleanup(_lifecycle.Cancel);
        if (_root == this) TryCleanup(_frameClock.Dispose);
        _ = FinishCleanupAsync();
    }

    private void TryCleanup(Action cleanup)
    {
        try { cleanup(); } catch (Exception error) { Report(error, "Scope cleanup", CurrentOperation.Value); }
    }

    private async Task FinishCleanupAsync()
    {
        try
        {
            while (_tasks.Count > 0 || _children.Count > 0)
                await Task.WhenAll(_tasks.Select(t => t.Completion.Task).Concat(_children.Select(c => c._ended.Task)).ToArray()).ConfigureAwait(true);
            await _lifecycle.DrainAsync().ConfigureAwait(true);
        }
        finally
        {
            _parent?._children.Remove(this);
            _ended.TrySetResult();
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispatcher.UIThread.VerifyAccess();
        for (var operation = CurrentOperation.Value; operation is not null; operation = operation.Parent)
        {
            if (!operation.Active) continue;
            for (var scope = operation.Scope; scope is not null; scope = scope._parent)
                if (ReferenceEquals(scope, this))
                    throw new InvalidOperationException("An animation operation cannot await disposal of its own scope or an ancestor.");
        }
        Dispose();
        return new ValueTask(_ended.Task);
    }

    // Not async: it hands out the FrameClock wait itself, so callers resume inline in the batch
    // regardless of the context they awaited from (an async layer here would reintroduce that).
    private sealed class ScopeClock(AnimationScope owner) : IAnimationFrameClock, IAnimationLifetimeClock, IFrameWaitProvider
    {
        public AnimationLifetime AnimationLifetime => owner._lifecycle;
        public double DurationScale => owner.DurationScale * owner.Host.GetValue(Animated.DurationScaleProperty);
        public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken) => NextFrameAsync(cancellationToken, null);
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken, Action<Exception>? onFault)
        {
            try
            {
                owner.VerifyActive();
                return owner._frameClock.WaitAsync(cancellationToken, owner.Lifetime, onFault).AsValueTask();
            }
            catch (Exception error)
            {
                // Same observable shape as before: failures surface from the returned task.
                onFault?.Invoke(error);
                return ValueTask.FromException<TimeSpan>(error);
            }
        }
    }

    private sealed class ScopeResource(AnimationScope owner, Action dispose, bool binding) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose()
        {
            Dispatcher.UIThread.VerifyAccess();
            var action = _dispose;
            _dispose = null;
            (binding ? owner._bindings : owner._subscriptions).Remove(this);
            action?.Invoke();
        }
    }

    private sealed class Operation(AnimationScope scope, Operation? parent, string? label)
    {
        internal Guid Id { get; } = Guid.NewGuid();
        internal AnimationScope Scope { get; } = scope;
        internal Operation? Parent { get; } = parent;
        internal string? Label { get; } = label;
        internal bool Active = true;
        internal Task Task = null!;
        internal TaskCompletionSource Completion { get; } = new();
    }

    private sealed record Failure(Guid Id, HashSet<Guid> Ancestors, bool FrameSource, string? Trace);
}
