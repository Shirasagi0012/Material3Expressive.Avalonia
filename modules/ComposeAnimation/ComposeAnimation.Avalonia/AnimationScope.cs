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

/// <summary>Describes one animation failure, raised by the root scope's <see cref="AnimationScope.AnimationFailed"/>.</summary>
public sealed class AnimationFailureEventArgs : EventArgs
{
    internal AnimationFailureEventArgs(Guid id, AnimationScope source, string? label, Exception error, Visual? host = null)
    {
        FailureId = id;
        SourceScope = source;
        Host = host ?? source.Host;
        OperationLabel = label;
        Exception = error;
    }

    /// <summary>Identity of the failure; the same failure observed again keeps its id.</summary>
    public Guid FailureId { get; }
    /// <summary>The scope that reported the failure.</summary>
    public AnimationScope SourceScope { get; }
    /// <summary>The visual the failure belongs to (the source scope's host unless reported for another visual).</summary>
    public Visual Host { get; }
    /// <summary>The label of the failing operation, when one was given.</summary>
    public string? OperationLabel { get; }
    /// <summary>The original exception.</summary>
    public Exception Exception { get; }
}

/// <summary>Arguments of <see cref="AnimationScope.ScopeChangedEvent"/>.</summary>
/// <remarks><see cref="RoutedEventArgs.Source"/> is the host for <see cref="Interactive"/> hosts and null otherwise.</remarks>
public sealed class AnimationScopeChangedEventArgs : RoutedEventArgs
{
    internal AnimationScopeChangedEventArgs(AnimationScope? oldScope, AnimationScope? newScope)
        : base(AnimationScope.ScopeChangedEvent)
    {
        OldScope = oldScope;
        NewScope = newScope;
    }

    /// <summary>The scope that ended.</summary>
    public AnimationScope? OldScope { get; }
    /// <summary>The replacement scope, or null when the control left the tree.</summary>
    public AnimationScope? NewScope { get; }
}

/// <summary>
/// Owns animation work, samples and children within a fixed TopLevel environment. Obtain a
/// control's scope with <see cref="Get"/>; the window root comes from <see cref="Initialize(TopLevel, IFrameSource?)"/>.
/// </summary>
public class AnimationScope : FrameScope
{
    /// <summary>
    /// How a scope is created and what ends it.
    /// <code>
    /// Kind       created by                  parent              ends on                          rebuilt by drain
    /// Root       Initialize                  none                window Closed                    no
    /// PopupRoot  Resolve (popup TopLevel)    parent TopLevel's   popup Closed / visual detach     no
    /// Owned      Get                         ancestor or root    drain (detach/move), parent end  yes (stale -> Kept/Replaced/Ended/Superseded/Deferred)
    /// Child      CreateChild                 creating scope      Dispose, parent end              no
    /// </code>
    /// </summary>
    private enum Kind { Root, PopupRoot, Owned, Child }

    private enum RebuildResult { Kept, Replaced, Ended, Superseded, Deferred }

    private static readonly ConditionalWeakTable<TopLevel, AnimationScope> Roots = new();
    private static readonly ConditionalWeakTable<Visual, List<HandlerEntry>> Handlers = new();
    private static readonly AsyncLocal<Operation?> CurrentOperation = new();

    /// <summary>The scope owned by a control (local value); descendants inherit it as their parent.</summary>
    internal static readonly AttachedProperty<AnimationScope?> ScopeProperty =
        AvaloniaProperty.RegisterAttached<AnimationScope, Visual, AnimationScope?>("Scope", inherits: true);

    /// <summary>
    /// Raised directly on a control when the drain (or <see cref="Get"/>) replaced or ended its
    /// <see cref="Get"/> scope. Handlers added with <see cref="AddScopeChangedHandler"/> are isolated:
    /// one that throws is reported and the others still run.
    /// </summary>
    public static readonly RoutedEvent<AnimationScopeChangedEventArgs> ScopeChangedEvent =
        RoutedEvent.Register<AnimationScopeChangedEventArgs>("ScopeChanged", RoutingStrategies.Direct, typeof(AnimationScope));

    /// <summary>Inherited playback duration multiplier for the visual subtree. Raw frame timestamps are unchanged.</summary>
    public static readonly AttachedProperty<double> DurationScaleProperty =
        AvaloniaProperty.RegisterAttached<AnimationScope, Visual, double>("DurationScale", 1, inherits: true,
            validate: IsValidScale);

    private readonly Kind _kind;
    private readonly AnimationScope? _parent;
    private readonly AnimationScope _root;
    private readonly StaleSet? _stale;
    private readonly FrameClock _frameClock;
    private readonly RunTracker _lifecycle;
    private readonly CancellationToken _token;
    private readonly HashSet<AnimationScope> _children = [];
    private readonly HashSet<Operation> _tasks = [];
    private readonly HashSet<ScopeResource> _subscriptions = [];
    private readonly HashSet<ScopeResource> _bindings = [];
    private readonly HashSet<PlaybackEntry> _playback = [];
    private readonly ConditionalWeakTable<Exception, Failure> _failures;
    private readonly TaskCompletionSource _ended = new();
    private bool _ending;
    private bool _cleanupStarted;
    private bool _isStale;
    private bool _parked;
    private int _generation;
    private double? _durationScale;
    private Exception? _lastError;
    private Exception? _lastDiagnosticError;

    // Exists only so AnimationChildScope can derive; every other kind uses the private constructor.
    private protected AnimationScope(Visual host, AnimationScope parent, string? label)
        : this(Kind.Child, host, parent, null, label) { }

    private AnimationScope(Kind kind, Visual host, AnimationScope? parent, IFrameSource? frameSource, string? label,
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
        _frameClock = parent?._frameClock ?? new FrameClock(frameSource!,
            onSourceFailure: error => Report(error, "Frame source", null, frameSource: true));
        _failures = _root == this ? new() : _root._failures;
        _lifecycle = new RunTracker(this);
        _token = _lifecycle.Token;
        _lifecycle.UnmanagedFailure = error =>
        {
            if (CurrentOperation.Value?.Active != true) ReportFailure(error);
        };
        parent?._children.Add(this);
        if (_root == this)
        {
            _stale = new StaleSet(this);
            _stale.Hook = _frameClock.RegisterBeforeFrame(DrainStale);
        }
        if (kind != Kind.Child) host.PropertyChanged += OnHostPropertyChanged;
        switch (kind)
        {
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

    // ---- Public surface -------------------------------------------------------------------------

    public Visual Host { get; }
    public string? Label { get; }

    /// <summary>Coroutine delay time source, inherited by children; independent of frame timestamps and DurationScale.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>The scope's only cancellation signal; cancelled when the scope ends.</summary>
    public override CancellationToken Lifetime => _token;

    public event EventHandler<AnimationFailureEventArgs>? AnimationFailed;

    /// <summary>
    /// Inherited playback duration multiplier, multiplied by the host's attached
    /// <see cref="DurationScaleProperty"/>. Raw frame timestamps are unchanged. A value set here on a
    /// <see cref="Get"/> scope is lost when that scope is rebuilt.
    /// </summary>
    public override double DurationScale
    {
        get => _durationScale ?? _parent?.DurationScale ?? 1;
        set
        {
            VerifyActive();
            if (!IsValidScale(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _durationScale = value;
            PlaybackChanged();
        }
    }

    public static double GetDurationScale(Visual visual) => visual.GetValue(DurationScaleProperty);
    public static void SetDurationScale(Visual visual, double value) => visual.SetValue(DurationScaleProperty, value);

    public static AnimationScope Initialize(TopLevel host, IFrameSource? source = null)
        => Initialize(host, source, System.TimeProvider.System);

    /// <summary>Initializes an environment with an explicit ordinary-delay scheduler.</summary>
    public static AnimationScope Initialize(TopLevel host, IFrameSource? source, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(timeProvider);
        Dispatcher.UIThread.VerifyAccess();
        if (Roots.TryGetValue(host, out _)) throw new InvalidOperationException("This TopLevel already has an animation environment.");
        source ??= new AvaloniaFrameSource(host);
        source.VerifyAccess();
        var root = new AnimationScope(Kind.Root, host, null, source, "Root", timeProvider);
        Roots.Add(host, root);
        host.Closed += root.OnClosed;
        return root;
    }

    /// <summary>
    /// Returns the scope owned by <paramref name="control"/>, creating it on first use. Its parent is
    /// the nearest ancestor's scope (through the inheritance chain, popups included), or the window
    /// root. When the control moves in the tree the scope is marked stale and replaced (or ended)
    /// once, coalesced, before the next frame's waiters resume; <see cref="ScopeChangedEvent"/> then
    /// fires on the control. Until then <c>Get</c> keeps returning the current scope, unless that
    /// scope already ended (its parent ended): then it is rebuilt synchronously here, raising
    /// <see cref="ScopeChangedEvent"/>, so <c>Get</c> never returns an ended scope. A move made
    /// while a frame's waiters are resuming takes effect for the next frame.
    /// </summary>
    public static AnimationScope Get(Visual control)
    {
        ArgumentNullException.ThrowIfNull(control);
        Dispatcher.UIThread.VerifyAccess();
        if (OwnedScopeOf(control) is { } current)
        {
            if (!current._ending) return current;
            current.RebuildNow();
            if (OwnedScopeOf(control) is { _ending: false } rebuilt) return rebuilt;
        }
        var parent = ResolveParent(control) ?? throw new InvalidOperationException("Animation scopes require an attached Visual with a TopLevel.");
        parent.VerifyActive();
        var scope = new AnimationScope(Kind.Owned, control, parent, null, control.GetType().Name);
        control.SetValue(ScopeProperty, scope);
        return scope;
    }

    /// <summary>Adds a <see cref="ScopeChangedEvent"/> handler. UI thread only. For a host that is not
    /// <see cref="Interactive"/>, the event's <see cref="RoutedEventArgs.Source"/> is null.</summary>
    public static void AddScopeChangedHandler(Visual control, EventHandler<AnimationScopeChangedEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(handler);
        Dispatcher.UIThread.VerifyAccess();
        var entry = new HandlerEntry(handler, control);
        Handlers.GetOrCreateValue(control).Add(entry);
        if (control is Interactive interactive) interactive.AddHandler(ScopeChangedEvent, entry.Invoke);
    }

    /// <summary>Removes a handler added with <see cref="AddScopeChangedHandler"/>. UI thread only.</summary>
    public static void RemoveScopeChangedHandler(Visual control, EventHandler<AnimationScopeChangedEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(handler);
        Dispatcher.UIThread.VerifyAccess();
        if (!Handlers.TryGetValue(control, out var list)) return;
        var index = list.FindIndex(e => e.Handler == handler);
        if (index < 0) return;
        var entry = list[index];
        list.RemoveAt(index);
        if (control is Interactive interactive) interactive.RemoveHandler(ScopeChangedEvent, entry.Invoke);
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
        // Argument errors throw synchronously, like Task.Delay, instead of faulting the scope.
        if (delay != Timeout.InfiniteTimeSpan) ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        return Launch(async lifetime =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellationToken);
            var token = linked.Token;
            token.ThrowIfCancellationRequested();
            if (delay > TimeSpan.Zero || delay == Timeout.InfiniteTimeSpan)
            {
                // The timer may fire on any thread: completion is posted to the UI thread, so the
                // continuation never runs off-thread, and a cancellation that lands while that post
                // is queued wins (cancellable suspension), whatever context the caller awaited from.
                // Asynchronous continuations: TrySetResult never runs the awaiting coroutine inline
                // inside the posted callback. The trailing token check keeps cancellation winning.
                var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var timer = TimeProvider.CreateTimer(
                    static state => Dispatcher.UIThread.Post(CompleteDelay, state, FrameClock.ResumePriority),
                    done, delay, Timeout.InfiniteTimeSpan);
                using var registration = token.Register(static state =>
                {
                    var (source, ct) = ((TaskCompletionSource, CancellationToken))state!;
                    if (Dispatcher.UIThread.CheckAccess()) source.TrySetCanceled(ct);
                    else Dispatcher.UIThread.Post(() => source.TrySetCanceled(ct), FrameClock.ResumePriority);
                }, (done, token));
                await done.Task;
            }
            token.ThrowIfCancellationRequested();
        }, "Coroutine delay");
    }

    private static void CompleteDelay(object? state) => ((TaskCompletionSource)state!).TrySetResult();

    // ---- Internal surface -----------------------------------------------------------------------

    /// <summary>The window's frame clock, shared by every scope of this environment.</summary>
    internal FrameClock FrameClock => _frameClock;
    internal override bool IsEnding => _ending;

    // ---- FrameScope: what runs need ----

    // Not async: it hands out the FrameClock wait itself, so callers resume inline in the batch
    // regardless of the context they awaited from (an async layer here would reintroduce that).
    internal override ValueTask<TimeSpan> WaitFrameAsync(CancellationToken cancellationToken, Action<Exception>? onFault)
    {
        try
        {
            VerifyActive();
            return _frameClock.WaitAsync(cancellationToken, _token, onFault).AsValueTask();
        }
        catch (Exception error)
        {
            // Same observable shape as before: failures surface from the returned task.
            onFault?.Invoke(error);
            return ValueTask.FromException<TimeSpan>(error);
        }
    }

    internal override double EffectiveDurationScale => DurationScale * Host.GetValue(DurationScaleProperty);
    internal override void VerifyAccess() => VerifyActive();
    internal override Task<T> Track<T>(Func<Task<T>> run) => _lifecycle.Track(run);
    internal override Task Track(Func<Task> run) => _lifecycle.Track(run);
    internal override void ReportRunFailure(Exception error) => ReportFailure(error);
    internal override IDisposable RegisterPlaybackChanged<T>(T owner, Action<T> changed)
    {
        var weak = new WeakReference<T>(owner);
        var entry = new PlaybackEntry(this, () => { if (weak.TryGetTarget(out var target)) changed(target); });
        if (!_ending) _playback.Add(entry);
        return entry;
    }
    internal ScopeDiagnostics Diagnostics => new(this);

    /// <summary>
    /// Called by an <see cref="IAnimationScopeHost"/> when <see cref="IAnimationScopeHost.DefersScopeRebuild"/>
    /// may have turned false: a parked stale scope is queued for the drain again.
    /// </summary>
    internal static void RebuildPolicyChanged(Visual host)
    {
        if (OwnedScopeOf(host) is { _parked: true } scope && !Defers(scope)) scope.MarkStale();
    }

    internal static bool IsEnvironmentEnding(Visual owner)
        => TopLevel.GetTopLevel(owner) is { } host && Roots.TryGetValue(host, out var scope) && scope._root._ending;

    internal static void ReportError(Visual host, Exception error)
    {
        if (OwnedScopeOf(host) is { _ending: false } scope) scope.Report(error, null, CurrentOperation.Value);
        else if (TopLevel.GetTopLevel(host) is { } top) Resolve(top).Report(error, null, CurrentOperation.Value, host);
    }

    internal void VerifyActive()
    {
        Dispatcher.UIThread.VerifyAccess();
        ObjectDisposedException.ThrowIf(_ending, this);
    }

    internal void ReportFailure(Exception error, string? label = null) => Report(error, label, CurrentOperation.Value);

    internal IDisposable RegisterResource(Action dispose, bool binding = false)
    {
        VerifyActive();
        var resource = new ScopeResource(this, dispose, binding);
        (binding ? _bindings : _subscriptions).Add(resource);
        return resource;
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

    // ---- Lookup ---------------------------------------------------------------------------------

    private static bool IsValidScale(double value) => double.IsFinite(value) && value >= 0;

    private static AnimationScope? OwnedScopeOf(Visual control)
        => control.GetValue(ScopeProperty) is { _kind: Kind.Owned } scope && ReferenceEquals(scope.Host, control) ? scope : null;

    private static bool Defers(AnimationScope scope)
        => !scope._ending && scope.Host is IAnimationScopeHost { DefersScopeRebuild: true };

    // Null when the control is not attached to a TopLevel. The value is read one step above the
    // control, because the control's own local value shadows the inherited one (D13). It may be an
    // ancestor scope that already ended and awaits its own rebuild; callers decide.
    private static AnimationScope? ResolveParent(Visual control)
    {
        var top = TopLevel.GetTopLevel(control);
        if (top is null) return null;
        if (AvaloniaInternals.GetInheritanceParent(control) is { } above && above.GetValue(ScopeProperty) is { } inherited)
            return inherited;
        return Resolve(top);
    }

    private static AnimationScope Resolve(TopLevel host)
    {
        if (Roots.TryGetValue(host, out var root))
        {
            if (!root._ending || host is not PopupRoot) return root;
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

    // ---- Stale marking, drain and rebuild -------------------------------------------------------

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
    // A parked scope (deferred by its host's policy) is queued again.
    private void MarkStale()
    {
        if (_kind != Kind.Owned || _isStale && !_parked) return;
        _isStale = true;
        _parked = false;
        var set = _root._stale!;
        set.Scopes.Add(this);
        set.ArmFallback();
    }

    // Runs in the FrameClock before-frame hook (before the waiter snapshot, so restarted runs join
    // this frame, D16) and in the fallback post. Whichever runs first drains; the other is a no-op.
    // All buffers live on the StaleSet, so a drain with deferred entries does not allocate.
    private void DrainStale()
    {
        var set = _stale!;
        if (set.Scopes.Count == 0) return;
        if (set.Draining) return;
        set.Draining = true;
        var nextFrame = false;
        var retry = false;
        try
        {
            // Index loop: scopes marked during the drain (cascade) are handled in this drain too.
            for (var i = 0; i < set.Scopes.Count; i++)
            {
                var scope = set.Scopes[i];
                if (!scope._isStale || scope._parked) continue;
                // D18: at most one rebuild per host per drain; a host made stale again waits.
                if (set.Rebuilt.Contains(scope.Host))
                {
                    set.Deferred.Add(scope);
                    nextFrame = true;
                    continue;
                }
                bool defers;
                try { defers = Defers(scope); }
                catch (Exception error)
                {
                    scope.Report(error, "Scope rebuild", null);
                    defers = false;
                }
                if (defers)
                {
                    // Out of the queue until the host calls RebuildPolicyChanged (no per-frame polling).
                    scope._parked = true;
                    continue;
                }
                scope._isStale = false;
                RebuildResult result;
                try { result = scope.Rebuild(); }
                catch (Exception error)
                {
                    // Isolated: the rest of the window's stale scopes still rebuild.
                    scope.Report(error, "Scope rebuild", null);
                    continue;
                }
                switch (result)
                {
                    case RebuildResult.Replaced:
                    case RebuildResult.Ended:
                        set.Rebuilt.Add(scope.Host);
                        break;
                    case RebuildResult.Deferred:
                        scope._isStale = true;
                        set.Deferred.Add(scope);
                        retry = true;
                        break;
                }
            }
        }
        finally
        {
            set.Scopes.Clear();
            set.Scopes.AddRange(set.Deferred);
            set.Deferred.Clear();
            set.Rebuilt.Clear();
            set.Draining = false;
            // Deferred entries are never stranded: a host rebuilt in this drain waits for the next
            // frame (D18), so request one; an entry waiting on its ancestor retries in the fallback post.
            if (nextFrame) RequestFrame();
            if (retry) set.ArmFallback();
        }
    }

    private void RequestFrame()
    {
        if (_ending) return;
        try { _ = _frameClock.WaitAsync(_token); }
        catch (Exception error) { Report(error, "Scope rebuild", null); }
    }

    // Get on an ended scope: rebuild this one scope now (its ended ancestors first), outside the drain.
    private void RebuildNow()
    {
        if (ResolveParent(Host) is { _kind: Kind.Owned, _ending: true } parent && OwnedScopeOf(parent.Host) == parent)
            parent.RebuildNow();
        _isStale = false;
        _parked = false;
        var result = Rebuild();
        if (result is RebuildResult.Replaced or RebuildResult.Ended && _root._stale is { Draining: true } set)
            set.Rebuilt.Add(Host);
    }

    private RebuildResult Rebuild()
    {
        var host = Host;
        if (!ReferenceEquals(host.GetValue(ScopeProperty), this))
        {
            // The local value was replaced: nothing to keep alive.
            if (!_ending) End();
            return RebuildResult.Superseded;
        }
        AnimationScope? parent;
        try { parent = ResolveParent(host); }
        catch (Exception error)
        {
            Report(error, "Scope rebuild", null);
            parent = null;
        }
        if (parent is not null && !_ending && ReferenceEquals(parent, _parent)) return RebuildResult.Kept;
        // The ancestor is itself waiting for its rebuild (D18 deferral); follow it.
        if (parent is { _ending: true, _isStale: true, _kind: Kind.Owned }) return RebuildResult.Deferred;

        End();
        if (parent is not null && !parent._ending)
        {
            var replacement = new AnimationScope(Kind.Owned, host, parent, null, Label) { _generation = _generation + 1 };
            host.SetValue(ScopeProperty, replacement);
            RaiseScopeChanged(host, this, replacement);
            return RebuildResult.Replaced;
        }
        host.ClearValue(ScopeProperty);
        RaiseScopeChanged(host, this, null);
        return RebuildResult.Ended;
    }

    // The component's own reaction runs first and directly, independent of routed-event order;
    // then the routed event (Interactive) or the handler table (plain Visual). Each failure is
    // reported on the new scope (the old one when the control left the tree).
    private static void RaiseScopeChanged(Visual host, AnimationScope old, AnimationScope? replacement)
    {
        var args = new AnimationScopeChangedEventArgs(old, replacement) { Source = host as Interactive };
        if (host is IAnimationScopeHost component)
        {
            try { component.OnScopeChanged(args); }
            catch (Exception error) { ReportHandlerError(host, args, error); }
        }
        if (host is Interactive interactive)
        {
            // Handlers from AddScopeChangedHandler isolate themselves; this catches raw AddHandler ones.
            try { interactive.RaiseEvent(args); }
            catch (Exception error) { ReportHandlerError(host, args, error); }
        }
        else if (Handlers.TryGetValue(host, out var list))
        {
            foreach (var entry in list.ToArray()) entry.Invoke(host, args);
        }
    }

    private static void ReportHandlerError(Visual host, AnimationScopeChangedEventArgs args, Exception error)
        => (args.NewScope ?? args.OldScope)!.Report(error, "ScopeChanged handler", null, host);

    // ---- Ending ---------------------------------------------------------------------------------

    // One recursive pass over a snapshot of each level: mark ending, and (when this Get-owned scope
    // ends while still attached) mark Get-owned descendants stale so the drain rebuilds them under
    // their new parent.
    private void MarkEnding()
    {
        var restale = _kind == Kind.Owned && !_root._ending && TopLevel.GetTopLevel(Host) is not null;
        MarkEnding(restale, top: true);
    }

    private void MarkEnding(bool restale, bool top)
    {
        _ending = true;
        _lifecycle.MarkEnding();
        if (restale && !top) MarkStale();
        foreach (var child in _children.ToArray()) child.MarkEnding(restale, top: false);
    }

    private void PlaybackChanged()
    {
        if (!_ending)
            foreach (var entry in _playback.ToArray()) entry.Changed();
        foreach (var child in _children.ToArray()) child.PlaybackChanged();
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
        _playback.Clear();
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

    // ---- Tasks and failures ---------------------------------------------------------------------

    private async Task ObserveAsync(Operation entry)
    {
        try
        {
            // Observe without rethrowing: a rethrow here would append this method's frames to the
            // exception's stack, so a later await of the same task elsewhere (whose stack extends
            // the original throw, not ours) would no longer be recognised as the same failure.
            await entry.Task.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
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
        // Needed: a task launched earlier (not an ancestor operation) and later awaited by a
        // managed parent shares no operation chain with it; only the extended stack ties them
        // (AnimationScopeTest.PreviouslyLaunchedChildFailure_IsNotReportedAgainWhenAwaitedByManagedParent).
        // Await/EDI propagation preserves the previously thrown stack and extends it.
        // A fresh throw of a reused exception replaces that stack and remains independent.
        return !string.IsNullOrEmpty(origin) && error.StackTrace is { } trace &&
               trace.Length > origin.Length && trace.StartsWith(origin, StringComparison.Ordinal);
    }

    private static IEnumerable<Exception> Leaves(Exception error)
        => error is AggregateException { InnerExceptions.Count: > 0 } aggregate
            ? aggregate.InnerExceptions.SelectMany(Leaves) : [error];

    // ---- Nested types ---------------------------------------------------------------------------

    /// <summary>Internal diagnostics view of a scope (tests).</summary>
    internal readonly struct ScopeDiagnostics(AnimationScope scope)
    {
        public int ActiveTaskCount => scope._tasks.Count;
        public int ActiveChildCount => scope._children.Count;
        public int SubscriptionCount => scope._subscriptions.Count;
        public int BindingCount => scope._bindings.Count;
        public int PendingAnimationCount => scope._lifecycle.PendingCount;
        public Exception? LastError => scope._lastError;
        /// <summary>Last exception thrown by a diagnostic subscriber. Never recursively reported.</summary>
        public Exception? LastDiagnosticError => scope._lastDiagnosticError;
        public bool IsEnding => scope._ending;
        public bool IsStale => scope._isStale;
        public bool IsParked => scope._parked;
        public AnimationScope? Parent => scope._parent;
        /// <summary>0 for a first scope; each rebuild's replacement is one higher.</summary>
        public int Generation => scope._generation;
        public int WindowStaleCount => scope._root._stale!.Scopes.Count;
        public Task Ended => scope._ended.Task;
        public Task EndAsync() => scope.EndAsync();
    }

    /// <summary>
    /// The scope's run bookkeeping: the lifetime token, the tracked runs (for
    /// <see cref="AnimationScopeDiagnostics.PendingAnimationCount"/> and the drain on end) and
    /// failure observation of unmanaged runs.
    /// </summary>
    private sealed class RunTracker
    {
        private readonly AnimationScope _owner;
        private readonly CancellationTokenSource _cancellation = new();
        private readonly HashSet<Task> _operations = [];
        private bool _ending;
        private bool _drained;

        internal RunTracker(AnimationScope owner)
        {
            _owner = owner;
            Token = _cancellation.Token;
        }

        internal CancellationToken Token { get; }
        internal int PendingCount => _operations.Count;
        internal Action<Exception>? UnmanagedFailure { get; set; }
        internal void MarkEnding() => _ending = true;
        internal void Cancel()
        {
            if (!_drained) _cancellation.Cancel();
        }

        // Track returns the operation's own task, so the typed overload is a cast.
        internal Task<T> Track<T>(Func<Task<T>> operation) => (Task<T>)Track((Func<Task>)operation);

        internal Task Track(Func<Task> operation)
        {
            // Name the owning scope, not this private helper, in the ObjectDisposedException.
            ObjectDisposedException.ThrowIf(_ending, _owner);
            var entry = new TaskCompletionSource();
            _operations.Add(entry.Task); // Before synchronous callbacks can dispose the scope.
            Task task;
            try { task = operation(); }
            catch { _operations.Remove(entry.Task); entry.TrySetResult(); throw; }
            _ = ObserveAsync(task, entry);
            return task;
        }

        private async Task ObserveAsync(Task task, TaskCompletionSource entry)
        {
            try
            {
                // Observe without rethrowing (see the operation observer): keeps the stack intact.
                await task.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
                if (task.IsFaulted)
                    foreach (var error in task.Exception!.InnerExceptions) UnmanagedFailure?.Invoke(error);
            }
            finally { _operations.Remove(entry.Task); entry.TrySetResult(); }
        }

        internal async Task DrainAsync()
        {
            while (_operations.Count > 0)
                try { await Task.WhenAll(_operations.ToArray()).ConfigureAwait(true); } catch { }
            // Drained: nothing can observe the token any more; release it exactly once.
            if (_drained) return;
            _drained = true;
            _cancellation.Dispose();
        }
    }

    private sealed class HandlerEntry(EventHandler<AnimationScopeChangedEventArgs> handler, Visual host)
    {
        public EventHandler<AnimationScopeChangedEventArgs> Handler { get; } = handler;

        public void Invoke(object? sender, AnimationScopeChangedEventArgs args)
        {
            try { Handler(sender ?? host, args); }
            catch (Exception error) { ReportHandlerError(host, args, error); }
        }
    }

    private sealed class PlaybackEntry(AnimationScope owner, Action changed) : IDisposable
    {
        public void Changed() => changed();
        public void Dispose() => owner._playback.Remove(this);
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

    // One per window, held on the root. Buffers are reused across drains.
    private sealed class StaleSet(AnimationScope root)
    {
        public readonly List<AnimationScope> Scopes = [];
        public readonly List<AnimationScope> Deferred = [];
        public readonly HashSet<Visual> Rebuilt = [];
        public IDisposable? Hook;
        public bool PostPending;
        public bool Draining;

        public void ArmFallback()
        {
            if (PostPending || Draining) return;
            PostPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                PostPending = false;
                root.DrainStale();
            }, FrameClock.ResumePriority);
        }
    }
}

/// <summary>A child scope from <see cref="AnimationScope.CreateChild"/>; dispose it to end only this child.</summary>
public sealed class AnimationChildScope : AnimationScope, IDisposable
{
    internal AnimationChildScope(Visual host, AnimationScope parent, string? label) : base(host, parent, label) { }

    /// <summary>
    /// Ends this child and its descendants. Must be called on the UI thread. Idempotent; a no-op
    /// once the parent ended it.
    /// </summary>
    public void Dispose() => End();
}
