using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
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

/// <summary>Arguments of <see cref="AnimationScope.ScopeChangedEvent"/>.</summary>
public sealed class AnimationScopeChangedEventArgs : RoutedEventArgs
{
    internal AnimationScopeChangedEventArgs(AnimationScope? oldScope, AnimationScope? newScope)
        : base(AnimationScope.ScopeChangedEvent) => (OldScope, NewScope) = (oldScope, newScope);
    /// <summary>The scope that ended.</summary>
    public AnimationScope? OldScope { get; }
    /// <summary>The replacement scope, or null when the control left the tree.</summary>
    public AnimationScope? NewScope { get; }
}

/// <summary>
/// Owns animation work, samples and children within a fixed TopLevel environment. Obtain a
/// control's scope with <see cref="Get"/>; the window root comes from <see cref="Initialize(TopLevel, IAnimationFrameClock?)"/>.
/// </summary>
public class AnimationScope
{
    private enum Kind { Root, PopupRoot, Owned, Detached, Child }

    private static readonly ConditionalWeakTable<TopLevel, AnimationScope> Roots = new();
    private static readonly ConditionalWeakTable<Visual, List<EventHandler<AnimationScopeChangedEventArgs>>> VisualHandlers = new();

    /// <summary>The scope owned by a control (local value); descendants inherit it as their parent.</summary>
    internal static readonly AttachedProperty<AnimationScope?> ScopeProperty =
        AvaloniaProperty.RegisterAttached<AnimationScope, Visual, AnimationScope?>("Scope", inherits: true);

    /// <summary>Raised directly on a control when the drain replaced or ended its <see cref="Get"/> scope.</summary>
    public static readonly RoutedEvent<AnimationScopeChangedEventArgs> ScopeChangedEvent =
        RoutedEvent.Register<AnimationScopeChangedEventArgs>("ScopeChanged", RoutingStrategies.Direct, typeof(AnimationScope));

    /// <summary>Inherited playback duration multiplier for the visual subtree. Raw frame timestamps are unchanged.</summary>
    public static readonly AttachedProperty<double> DurationScaleProperty =
        AvaloniaProperty.RegisterAttached<AnimationScope, Visual, double>("DurationScale", 1, inherits: true,
            validate: value => double.IsFinite(value) && value >= 0);
    public static double GetDurationScale(Visual visual) => visual.GetValue(DurationScaleProperty);
    public static void SetDurationScale(Visual visual, double value) => visual.SetValue(DurationScaleProperty, value);

    // The exact Avalonia inheritance parent (logical parent, or the ContentPresenter / popup parent
    // that Avalonia substitutes), so the lookup matches what inherited values see (D13).
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_InheritanceParent")]
    private static extern AvaloniaObject? InheritanceParentOf(AvaloniaObject element);

    private readonly Kind _kind;
    private readonly StaleSet? _stale;
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
    private bool _isStale;
    private int _rebuildCount;
    private bool _ending;
    private bool _cleanupStarted;
    private double? _durationScale;

    private protected AnimationScope(Visual host, AnimationScope? parent, IAnimationFrameClock? clock, string? label,
        TimeProvider? timeProvider = null) : this(Kind.Child, host, parent, clock, label, timeProvider) { }

    private AnimationScope(Kind kind, Visual host, AnimationScope? parent, IAnimationFrameClock? clock, string? label,
        TimeProvider? timeProvider = null)
    {
        _kind = kind;
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
        if (_root == this)
        {
            _stale = new StaleSet();
            _stale.Hook = _frameClock.RegisterBeforeFrame(DrainStale);
        }
        if (kind != Kind.Child) host.PropertyChanged += OnHostPropertyChanged;
        switch (kind)
        {
            case Kind.Detached:
            case Kind.PopupRoot:
                host.DetachedFromVisualTree += OnDetached;
                break;
            case Kind.Owned:
                host.AttachedToLogicalTree += OnLogicalTreeChanged;
                host.DetachedFromLogicalTree += OnLogicalTreeChanged;
                host.DetachedFromVisualTree += OnDetached;
                break;
        }
    }

    public Visual Host { get; }
    public string? Label { get; }

    /// <summary>
    /// Transitional: the frame clock that runtime state types still capture. Removed in stage 3,
    /// when runs take a scope instead.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IAnimationFrameClock Clock { get; }
    /// <summary>The window's frame clock, shared by every scope of this environment.</summary>
    internal FrameClock FrameClock => _frameClock;
    /// <summary>Coroutine delay time source, inherited by children; independent of frame timestamps and DurationScale.</summary>
    public TimeProvider TimeProvider { get; }
    /// <summary>The scope's only cancellation signal; cancelled when the scope ends.</summary>
    public CancellationToken Lifetime => _token;
    private Exception? _lastError;
    /// <summary>Last exception thrown by a diagnostic subscriber. Never recursively reported.</summary>
    private Exception? _lastDiagnosticError;
    public event EventHandler<AnimationFailureEventArgs>? AnimationFailed;
    internal bool IsEnding => _ending;
    internal AnimationScopeDiagnostics Diagnostics => new(this);

    /// <summary>
    /// Inherited playback duration multiplier, multiplied by the host's attached
    /// <see cref="DurationScaleProperty"/>. Raw frame timestamps are unchanged. A value set here on a
    /// <see cref="Get"/> scope is lost when that scope is rebuilt; use the attached property for a
    /// durable setting (or set it again from a <see cref="ScopeChangedEvent"/> handler).
    /// </summary>
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

    public static AnimationScope Initialize(TopLevel host, IAnimationFrameClock? source = null)
        => Initialize(host, source, System.TimeProvider.System);

    /// <summary>Initializes an environment with an explicit ordinary-delay scheduler.</summary>
    public static AnimationScope Initialize(TopLevel host, IAnimationFrameClock? source, TimeProvider timeProvider)
    {
        var clock = source;
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(timeProvider);
        Dispatcher.UIThread.VerifyAccess();
        if (Roots.TryGetValue(host, out _)) throw new InvalidOperationException("This TopLevel already has an animation environment.");
        clock ??= new AvaloniaAnimationFrameClock(host);
        clock.VerifyAccess();
        var root = new AnimationScope(Kind.Root, host, null, clock, "Root", timeProvider);
        Roots.Add(host, root);
        host.Closed += root.OnClosed;
        return root;
    }

    /// <summary>
    /// Returns the scope owned by <paramref name="control"/>, creating it on first use. Its parent is
    /// the nearest ancestor's scope (through the inheritance chain, popups included), or the window
    /// root. When the control moves in the tree the scope is marked stale and replaced (or ended)
    /// once, coalesced, before the next frame's waiters resume; <see cref="ScopeChangedEvent"/> then
    /// fires on the control. Until then <c>Get</c> keeps returning the current scope. A move made
    /// while a frame's waiters are resuming takes effect for the next frame.
    /// </summary>
    public static AnimationScope Get(Visual control)
    {
        ArgumentNullException.ThrowIfNull(control);
        Dispatcher.UIThread.VerifyAccess();
        if (control.GetValue(ScopeProperty) is { } current && ReferenceEquals(current.Host, control) &&
            current._kind == Kind.Owned && (!current._ending || current._isStale))
            return current;
        var parent = ResolveParent(control) ?? throw new InvalidOperationException("Animation scopes require an attached Visual with a TopLevel.");
        parent.VerifyActive();
        var scope = new AnimationScope(Kind.Owned, control, parent, null, control.GetType().Name);
        control.SetValue(ScopeProperty, scope);
        return scope;
    }

    // Null when the control is not attached to a TopLevel. The value is read one step above the
    // control, because the control's own local value shadows the inherited one (D13). It may be an
    // ancestor scope that already ended and awaits its own rebuild; callers decide.
    private static AnimationScope? ResolveParent(Visual control)
    {
        var top = TopLevel.GetTopLevel(control);
        if (top is null) return null;
        if (InheritanceParentOf(control) is { } above && above.GetValue(ScopeProperty) is { } inherited)
            return inherited;
        return Resolve(top);
    }

    /// <summary>
    /// A scope parented to the window root that ends when <paramref name="owner"/> detaches; not
    /// stored in the tree. For hosts whose scope ownership is still undecided (O5).
    /// </summary>
    internal static AnimationScope CreateDetached(Visual owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        Dispatcher.UIThread.VerifyAccess();
        var host = TopLevel.GetTopLevel(owner) ?? throw new InvalidOperationException("Animation scopes require an attached Visual with a TopLevel.");
        var environment = Resolve(host);
        environment.VerifyActive();
        return new AnimationScope(Kind.Detached, owner, environment, null, owner.GetType().Name);
    }

    public static void AddScopeChangedHandler(Visual control, EventHandler<AnimationScopeChangedEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(handler);
        if (control is Interactive interactive) interactive.AddHandler(ScopeChangedEvent, handler);
        else VisualHandlers.GetOrCreateValue(control).Add(handler);
    }

    public static void RemoveScopeChangedHandler(Visual control, EventHandler<AnimationScopeChangedEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(handler);
        if (control is Interactive interactive) interactive.RemoveHandler(ScopeChangedEvent, handler);
        else if (VisualHandlers.TryGetValue(control, out var list)) list.Remove(handler);
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
            root = new AnimationScope(Kind.PopupRoot, host, environment, null, "Popup");
            Roots.Add(host, root);
            host.Closed += root.OnClosed;
            return root;
        }
        throw new InvalidOperationException("Call AnimationScope.Initialize(topLevel, clock) before loading or setting its content.");
    }

    /// <summary>
    /// Creates a child scope with the same host. Dispose it to end only the child and its
    /// descendants; it also ends with this scope (including when this scope is rebuilt).
    /// </summary>
    public AnimationChildScope CreateChild(string? label = null)
    {
        VerifyActive();
        return new AnimationChildScope(Host, this, label);
    }

    /// <summary>
    /// Waits for the window's next frame. Cancelled by <paramref name="cancellationToken"/> or when
    /// this scope ends. The continuation resumes inline inside the frame batch.
    /// </summary>
    public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken = default)
        => ((ScopeClock)Clock).NextFrameAsync(cancellationToken, null);

    internal static bool IsEnvironmentEnding(Visual owner)
        => TopLevel.GetTopLevel(owner) is { } host && Roots.TryGetValue(host, out var scope) && scope._root.IsEnding;

    internal static void ReportError(Visual host, Exception error)
    {
        if (host.GetValue(ScopeProperty) is { } scope && ReferenceEquals(scope.Host, host) && !scope._ending)
            scope.Report(error, null, CurrentOperation.Value);
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
        _lastError = error;
        _root._lastError = error;
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
                    catch (Exception diagnosticError) { _root._lastDiagnosticError = diagnosticError; }
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
        if (_kind == Kind.Owned) MarkStale();
        else End();
    }
    private void OnLogicalTreeChanged(object? sender, LogicalTreeAttachmentEventArgs e) => MarkStale();
    private void OnClosed(object? sender, EventArgs e) => End();
    private void OnHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == DurationScaleProperty) PlaybackChanged();
    }

    // Mark only; the drain rebuilds (D14). Coalesced: one set per window, one fallback post.
    private void MarkStale()
    {
        if (_kind != Kind.Owned || _isStale) return;
        _isStale = true;
        var set = _root._stale!;
        set.Scopes.Add(this);
        if (set.PostPending || set.Draining) return;
        set.PostPending = true;
        var root = _root;
        Dispatcher.UIThread.Post(() =>
        {
            set.PostPending = false;
            root.DrainStale();
        }, FrameClock.ResumePriority);
    }

    // Runs in the FrameClock before-frame hook (before the waiter snapshot, so restarted runs join
    // this frame, D16) and in the fallback post. Whichever runs first drains; the other is a no-op.
    private void DrainStale()
    {
        var set = _stale!;
        if (set.Scopes.Count == 0) return;
        if (set.Draining) return;
        set.Draining = true;
        HashSet<Visual>? rebuilt = null;
        List<AnimationScope>? deferred = null;
        try
        {
            // Index loop: scopes marked during the drain (cascade) are handled in this drain too.
            for (var i = 0; i < set.Scopes.Count; i++)
            {
                var scope = set.Scopes[i];
                if (!scope._isStale) continue;
                // D18: at most one rebuild per host per frame; a host made stale again waits.
                if (rebuilt?.Contains(scope.Host) == true ||
                    scope.Host is AnimatedBounds { RetainsAnimationAttachment: true } && TopLevel.GetTopLevel(scope.Host) is null)
                {
                    (deferred ??= []).Add(scope);
                    continue;
                }
                scope._isStale = false;
                switch (scope.Rebuild())
                {
                    case RebuildResult.Replaced: (rebuilt ??= []).Add(scope.Host); break;
                    case RebuildResult.Deferred:
                        scope._isStale = true;
                        (deferred ??= []).Add(scope);
                        break;
                }
            }
        }
        finally
        {
            set.Scopes.Clear();
            if (deferred is not null) set.Scopes.AddRange(deferred);
            set.Draining = false;
        }
    }

    private enum RebuildResult { Kept, Replaced, Deferred }

    private RebuildResult Rebuild()
    {
        var host = Host;
        if (!ReferenceEquals(host.GetValue(ScopeProperty), this))
        {
            // Superseded (the local value was replaced): nothing to keep alive.
            if (!_ending) End();
            return RebuildResult.Kept;
        }
        AnimationScope? parent;
        try { parent = ResolveParent(host); }
        catch (Exception error)
        {
            Report(error, "Scope rebuild", null);
            parent = null;
        }
        if (parent is not null && !_ending && ReferenceEquals(parent, _parent)) return RebuildResult.Kept;
        // The ancestor is itself waiting for its rebuild (D18 deferral); follow it next frame.
        if (parent is { _ending: true, _isStale: true, _kind: Kind.Owned }) return RebuildResult.Deferred;

        End();
        AnimationScope? replacement = null;
        if (parent is not null && !parent._ending)
        {
            replacement = new AnimationScope(Kind.Owned, host, parent, null, Label) { _rebuildCount = _rebuildCount + 1 };
            host.SetValue(ScopeProperty, replacement);
        }
        else host.ClearValue(ScopeProperty);
        RaiseScopeChanged(host, this, replacement);
        return RebuildResult.Replaced;
    }

    private static void RaiseScopeChanged(Visual host, AnimationScope old, AnimationScope? replacement)
    {
        var args = new AnimationScopeChangedEventArgs(old, replacement) { Source = host as Interactive };
        try
        {
            if (host is Interactive interactive) interactive.RaiseEvent(args);
            else if (VisualHandlers.TryGetValue(host, out var list))
                foreach (var handler in list.ToArray()) handler(host, args);
        }
        catch (Exception error)
        {
            (replacement ?? old).Report(error, "ScopeChanged handler", null, host);
        }
    }
    private void PlaybackChanged()
    {
        _lifecycle.PlaybackChanged();
        foreach (var child in _children.ToArray()) child.PlaybackChanged();
    }

    /// <summary>
    /// Ends this scope and its descendants: mark ending, release subscriptions, then bindings,
    /// cancel, wait for tracked tasks and children, drain, remove from parent. Used by rebuild,
    /// detach, child disposal and window close.
    /// </summary>
    internal void End()
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
        // Cascade: Get-owned descendants are rebuilt under their new parent by the drain.
        if (_kind == Kind.Owned && !_root._ending && TopLevel.GetTopLevel(Host) is not null)
            foreach (var child in _children) child.MarkStaleAfterParentEnded();
    }

    private void MarkStaleAfterParentEnded()
    {
        if (_kind == Kind.Owned) MarkStale();
        foreach (var child in _children) child.MarkStaleAfterParentEnded();
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
        Host.AttachedToLogicalTree -= OnLogicalTreeChanged;
        Host.DetachedFromLogicalTree -= OnLogicalTreeChanged;
        Host.PropertyChanged -= OnHostPropertyChanged;
        if (_stale is not null) TryCleanup(_stale.Hook!.Dispose);
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

    internal Task Ended => _ended.Task;
    internal AnimationScope? Parent => _parent;
    internal bool IsStale => _isStale;
    internal int RebuildCount => _rebuildCount;
    internal int WindowStaleCount => _root._stale!.Scopes.Count;
    internal Exception? LastErrorCore => _lastError;
    internal Exception? LastDiagnosticErrorCore => _lastDiagnosticError;
    internal int TaskCount => _tasks.Count;
    internal int ChildCount => _children.Count;
    internal int SubscriptionCountCore => _subscriptions.Count;
    internal int BindingCountCore => _bindings.Count;
    internal int PendingAnimationCountCore => _lifecycle.PendingCount;

    /// <summary>Ends the scope and completes when its cleanup finished (tests).</summary>
    internal Task EndAsync()
    {
        Dispatcher.UIThread.VerifyAccess();
        for (var operation = CurrentOperation.Value; operation is not null; operation = operation.Parent)
        {
            if (!operation.Active) continue;
            for (var scope = operation.Scope; scope is not null; scope = scope._parent)
                if (ReferenceEquals(scope, this))
                    throw new InvalidOperationException("An animation operation cannot await disposal of its own scope or an ancestor.");
        }
        End();
        return _ended.Task;
    }

    // Not async: it hands out the FrameClock wait itself, so callers resume inline in the batch
    // regardless of the context they awaited from (an async layer here would reintroduce that).
    private sealed class ScopeClock(AnimationScope owner) : IAnimationFrameClock, IAnimationLifetimeClock, IFrameWaitProvider
    {
        public AnimationLifetime AnimationLifetime => owner._lifecycle;
        public double DurationScale => owner.DurationScale * owner.Host.GetValue(DurationScaleProperty);
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

    private sealed class StaleSet
    {
        public readonly List<AnimationScope> Scopes = [];
        public IDisposable? Hook;
        public bool PostPending;
        public bool Draining;
    }
}

/// <summary>A child scope from <see cref="AnimationScope.CreateChild"/>; dispose it to end only this child.</summary>
public sealed class AnimationChildScope : AnimationScope, IDisposable
{
    internal AnimationChildScope(Visual host, AnimationScope parent, string? label) : base(host, parent, null, label) { }

    /// <summary>Ends this child and its descendants. Idempotent; a no-op once the parent ended it.</summary>
    public void Dispose() => End();
}

/// <summary>Internal diagnostics view of a scope (tests).</summary>
internal readonly struct AnimationScopeDiagnostics(AnimationScope scope)
{
    public int ActiveTaskCount => scope.TaskCount;
    public int ActiveChildCount => scope.ChildCount;
    public int SubscriptionCount => scope.SubscriptionCountCore;
    public int BindingCount => scope.BindingCountCore;
    public int PendingAnimationCount => scope.PendingAnimationCountCore;
    public Exception? LastError => scope.LastErrorCore;
    public Exception? LastDiagnosticError => scope.LastDiagnosticErrorCore;
    public bool IsEnding => scope.IsEnding;
    public bool IsStale => scope.IsStale;
    public AnimationScope? Parent => scope.Parent;
    public int RebuildCount => scope.RebuildCount;
    public int WindowStaleCount => scope.WindowStaleCount;
    public Task Ended => scope.Ended;
    public Task EndAsync() => scope.EndAsync();
}
