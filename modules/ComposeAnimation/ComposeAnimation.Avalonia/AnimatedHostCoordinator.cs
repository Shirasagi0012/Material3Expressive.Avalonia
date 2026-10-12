using System.Collections.Specialized;
using System.Runtime.ExceptionServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal interface IPropertySession : IDisposable
{
    AvaloniaProperty Property { get; }
    void SetClock(FrameScope? clock, AnimationScope? scope = null);
    void ReconcileTarget();
    void Abort();
    void SnapToTarget();
}

internal sealed class AnimatedHostCoordinator : IDisposable
{
    private static readonly AttachedProperty<AnimatedHostCoordinator?> CoordinatorProperty =
        AvaloniaProperty.RegisterAttached<Visual, AnimatedHostCoordinator?>(
            "Coordinator", typeof(AnimatedHostCoordinator));

    private readonly Visual _host;
    // Null entries still own a declaration subscription while Property is unresolved.
    private readonly Dictionary<AnimatedProperty, IPropertySession?> _sessions = new();
    private readonly Queue<Action> _work = new();
    private AnimatedPropertyCollection? _collection;
    private AnimationScope? _motion;
    private ObservedFrameScope? _sessionClock;
    private IDisposable? _beforeFrame;
    private IDisposable? _afterWaiter;
    private bool _attached;
    private bool _processing;
    private bool _clockFailed;
    private bool _disposed;

    private AnimatedHostCoordinator(Visual host)
    {
        _host = host;
        _host.AttachedToVisualTree += OnAttached;
        _host.DetachedFromVisualTree += OnDetached;
        _host.PropertyChanged += OnHostPropertyChanged;
        _attached = TopLevel.GetTopLevel(host) is not null;
    }

    public static void Replace(Visual visual, AnimatedPropertyCollection? oldList,
        AnimatedPropertyCollection? newList)
    {
        if (ReferenceEquals(oldList, newList)) return;
        var coordinator = visual.GetValue(CoordinatorProperty);
        if (newList is null)
        {
            coordinator?.Dispose();
            visual.ClearValue(CoordinatorProperty);
            return;
        }

        if (coordinator is null)
        {
            coordinator = new AnimatedHostCoordinator(visual);
            visual.SetValue(CoordinatorProperty, coordinator);
        }

        coordinator.Execute(() => coordinator.SetCollection(newList));
    }

    public static void SnapToTarget(Visual visual, AvaloniaProperty property)
    {
        if (visual.GetValue(CoordinatorProperty) is not { } coordinator) return;
        foreach (var session in coordinator._sessions.Values.ToArray())
            if (session?.Property == property) coordinator.Guard(session, session.SnapToTarget);
    }

    private void SetCollection(AnimatedPropertyCollection collection)
    {
        if (_collection is not null)
            _collection.CollectionChanged -= OnCollectionChanged;
        ClearSessions();
        _collection = collection;
        _collection.CollectionChanged += OnCollectionChanged;
        foreach (var declaration in collection)
            AddDeclaration(declaration);
        ApplyClock();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Execute(() =>
        {
            if (!ReferenceEquals(sender, _collection)) return;
            if (e.Action == NotifyCollectionChangedAction.Move) return;
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearSessions();
                foreach (var declaration in _collection!)
                    AddDeclaration(declaration);
                return;
            }

            if (e.OldItems is not null)
                foreach (AnimatedProperty declaration in e.OldItems)
                    RemoveDeclaration(declaration);
            if (e.NewItems is not null)
                foreach (AnimatedProperty declaration in e.NewItems)
                    AddDeclaration(declaration);
        });

    private void AddDeclaration(AnimatedProperty declaration)
    {
        if (_sessions.ContainsKey(declaration))
            throw new InvalidOperationException("An animated declaration cannot occur twice on the same visual.");
        var session = CreateSession(declaration);
        _sessions.Add(declaration, session);
        declaration.PropertyChanged += OnDeclarationPropertyChanged;
        session?.SetClock(SessionClock, _motion);
    }

    private FrameScope? SessionClock => _clockFailed ? null : _sessionClock;

    private IPropertySession? CreateSession(AnimatedProperty declaration)
    {
        var property = declaration.Property;
        if (property is null) return null;
        foreach (var (existingDeclaration, session) in _sessions)
        {
            if (existingDeclaration != declaration && session?.Property == property)
                throw new InvalidOperationException($"An animated property is already registered for {property}.");
        }
        return declaration.CreateSession(_host);
    }

    private void RemoveDeclaration(AnimatedProperty declaration)
    {
        declaration.PropertyChanged -= OnDeclarationPropertyChanged;
        if (_sessions.Remove(declaration, out var session))
            session?.Dispose();
    }

    private void OnDeclarationPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != AnimatedProperty.PropertyProperty) return;
        Execute(() =>
        {
            var declaration = (AnimatedProperty)sender!;
            if (!_sessions.TryGetValue(declaration, out var previous)) return;
            var replacement = CreateSession(declaration);
            _sessions[declaration] = replacement;
            previous?.Dispose();
            replacement?.SetClock(SessionClock, _motion);
        });
    }

    private void ClearSessions()
    {
        var previous = _sessions.ToArray();
        _sessions.Clear();
        foreach (var (declaration, _) in previous)
            declaration.PropertyChanged -= OnDeclarationPropertyChanged;
        foreach (var (_, session) in previous)
            session?.Dispose();
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        => Execute(() => { _attached = true; ApplyClock(); });

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        => Execute(() => { _attached = false; ApplyClock(); });

    private void OnHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Priority <= BindingPriority.Animation) return;
        Execute(() =>
        {
            foreach (var session in _sessions.Values)
                if (session?.Property == e.Property)
                    Guard(session, session.ReconcileTarget);
        });
    }

    private void ApplyClock()
    {
        if (_attached && _motion is { IsEnding: false }) return;
        var previous = _motion;
        _motion = _attached ? AnimationScope.CreateDetached(_host) : null;
        _clockFailed = false;
        ReleaseFrameHooks();
        _sessionClock = _motion is null ? null : new ObservedFrameScope(_motion, (error, _) => { if (error is not OperationCanceledException) OnInnerFailure(error); });
        if (_motion is not null)
        {
            // The sessions wait directly on the window's FrameClock (through the scope clock).
            // Retargeting runs in its before-frame hook, i.e. after the frame arrives and before
            // any waiter of this frame (this host's included) samples, and replacement runs it
            // starts join this frame's batch.
            _beforeFrame = _motion.FrameClock.RegisterBeforeFrame(BeforeEachFrame);
            _afterWaiter = _motion.FrameClock.RegisterAfterWaiter(AfterEachWaiter);
        }
        foreach (var session in _sessions.Values) session?.SetClock(SessionClock, _motion);
        previous?.End();
    }

    private void ReleaseFrameHooks()
    {
        _beforeFrame?.Dispose();
        _afterWaiter?.Dispose();
        _beforeFrame = _afterWaiter = null;
    }

    private void BeforeEachFrame()
    {
        if (_disposed || _sessions.Count == 0 && _work.Count == 0) return;
        try
        {
            foreach (var session in _sessions.Values.ToArray())
            {
                if (session is null) continue;
                Guard(session, session.ReconcileTarget);
            }
            DrainWork();
        }
        catch (Exception error)
        {
            Dispatcher.UIThread.Post(() => ExceptionDispatchInfo.Capture(error).Throw());
        }
    }

    private void AfterEachWaiter()
    {
        if (_work.Count == 0) return;
        try
        {
            DrainWork();
        }
        catch (Exception error)
        {
            Dispatcher.UIThread.Post(() => ExceptionDispatchInfo.Capture(error).Throw());
        }
    }

    private void OnInnerFailure(Exception error)
        => Execute(() =>
        {
            if (_clockFailed) return;
            _clockFailed = true;
            foreach (var session in _sessions.Values)
                session?.SetClock(null);
            _motion?.ReportFailure(error);
        });

    // Property notifications can run application code. Defer structural changes until
    // the current operation has finished installing/publishing its binding.
    private void Execute(Action action)
    {
        if (_disposed) return;
        _work.Enqueue(action);
        if (_processing) return;
        _processing = true;
        try { DrainWork(); }
        finally { _processing = false; }
    }

    private void DrainWork()
    {
        while (_work.TryDequeue(out var action))
        {
            if (_disposed) { _work.Clear(); break; }
            action();
        }
    }

    private void Guard(IPropertySession session, Action action)
    {
        try { action(); }
        catch (Exception error)
        {
            session.Abort();
            _motion?.ReportFailure(error);
        }
    }

    public void Dispose() => Execute(() =>
    {
        _disposed = true;
        _host.AttachedToVisualTree -= OnAttached;
        _host.DetachedFromVisualTree -= OnDetached;
        _host.PropertyChanged -= OnHostPropertyChanged;
        if (_collection is not null)
            _collection.CollectionChanged -= OnCollectionChanged;
        ClearSessions();
        ReleaseFrameHooks();
        _sessionClock = null;
        _motion?.End();
        _motion = null;
    });
}
