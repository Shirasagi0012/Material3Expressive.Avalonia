using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// One shared-transition coordinate space. Overlay drawing stays in this host; endpoints in a
/// different host never pair. The caller still owns the parent Transition and its frame loop.
/// </summary>
public class SharedTransitionHost : ContentControl
{
    public static readonly StyledProperty<Transition?> TransitionProperty =
        AvaloniaProperty.Register<SharedTransitionHost, Transition?>(nameof(Transition));
    public static readonly StyledProperty<TimeSpan> PreparationTimeoutProperty =
        AvaloniaProperty.Register<SharedTransitionHost, TimeSpan>(nameof(PreparationTimeout),
            TimeSpan.FromMilliseconds(1200));

    private readonly Dictionary<SharedTransitionParticipant, SharedTransitionContent> _endpoints = [];
    private readonly List<string> _diagnostics = [];
    private SharedTransitionScope? _scope;
    private SharedTransitionCoordinator? _coordinator;
    private SharedVisualBackend? _backend;
    private SharedTransitionOverlay? _overlay;
    private DispatcherTimer? _deadline;
    private long _deadlineGeneration;
    private bool _attached;
    private bool _handlingLayout;

    static SharedTransitionHost()
    {
        HorizontalContentAlignmentProperty.OverrideDefaultValue<SharedTransitionHost>(HorizontalAlignment.Stretch);
        VerticalContentAlignmentProperty.OverrideDefaultValue<SharedTransitionHost>(VerticalAlignment.Stretch);
        TemplateProperty.OverrideDefaultValue<SharedTransitionHost>(new FuncControlTemplate<SharedTransitionHost>((host, names) =>
        {
            var presenter = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~ContentProperty] = new TemplateBinding(ContentProperty),
                [~ContentTemplateProperty] = new TemplateBinding(ContentTemplateProperty),
                [~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
                [~PaddingProperty] = new TemplateBinding(PaddingProperty),
                [~HorizontalContentAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty),
                [~VerticalContentAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty)
            };
            names.Register(presenter.Name, presenter);
            var overlay = new SharedTransitionOverlay
            {
                Name = "PART_Overlay",
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            names.Register(overlay.Name, overlay);
            var panel = new Panel();
            panel.Children.Add(presenter);
            panel.Children.Add(overlay);
            return panel;
        }));
    }

    public Transition? Transition { get => GetValue(TransitionProperty); set => SetValue(TransitionProperty, value); }
    public TimeSpan PreparationTimeout { get => GetValue(PreparationTimeoutProperty); set => SetValue(PreparationTimeoutProperty, value); }
    public Exception? LastPreparationError => _scope?.LastPreparationError;
    public Exception? LastSessionError => _scope?.LastSessionError;
    public SharedTransitionScope? Scope => _scope;
    public IReadOnlyList<string> Diagnostics => _diagnostics;
    public bool IsOverlayActive => _overlay?.Active == true;
    public bool IsPreparing => _scope?.IsPreparing == true;
    public int RegisteredEndpointCount => _endpoints.Count;
    public int ActiveSessionCount => _scope?.Sessions.Count(s => !s.IsClosed) ?? 0;
    public string EndpointStates => string.Join(" | ", _endpoints.Select(x =>
        $"{x.Value.Key}: {x.Key.Visibility.CurrentState}->{x.Key.Visibility.TargetState}"));
    public string LastPreparationParticipants => _coordinator?.LastRequestSummary ?? "-";
    internal SharedTransitionCoordinator? Coordinator => _coordinator;
    internal SharedTransitionOverlay? Overlay => _overlay;

    public SharedTransitionUpdate BeginUpdate()
    {
        var parent = Transition ?? throw new InvalidOperationException("Set Transition before BeginUpdate.");
        EnsureCoordinator(parent);
        return _scope!.BeginUpdate();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _overlay = e.NameScope.Find<SharedTransitionOverlay>("PART_Overlay")
            ?? throw new InvalidOperationException("SharedTransitionHost requires its default overlay template.");
        _overlay.IsHitTestVisible = false;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        LayoutUpdated += OnLayoutUpdated;
        if (Transition is not null) EnsureCoordinator(Transition);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        LayoutUpdated -= OnLayoutUpdated;
        Teardown();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != TransitionProperty) return;
        if (_coordinator is not null && (Transition is null || !ReferenceEquals(Transition, _scope!.ParentTransition)))
            throw new InvalidOperationException("Set Transition before the host starts shared preparation.");
        if (_attached && Transition is not null) EnsureCoordinator(Transition);
    }

    internal SharedTransitionContent? Endpoint(SharedTransitionParticipant participant)
        => _endpoints.TryGetValue(participant, out var endpoint) ? endpoint : null;

    internal SharedTransitionParticipant? Register(SharedTransitionContent endpoint, AnimatedVisibilityScope scope)
    {
        var parent = Transition ?? scope.Transition.ParentTransition;
        if (parent is null)
        {
            ReportDiagnostic(endpoint.Key, "Shared content needs a common parent Transition.");
            return null;
        }
        ApplyTemplate();
        EnsureCoordinator(parent);
        var participant = _coordinator!.Register(endpoint.Key!, scope.Transition, endpoint.ReadLayoutBounds,
            endpoint.IsLayoutAffectedByAncestorFlight);
        _endpoints[participant] = endpoint;
        participant.Unregistered += p => _endpoints.Remove(p);
        Dispatcher.UIThread.Post(() => _coordinator?.LayoutCompleted(), DispatcherPriority.Loaded);
        return participant;
    }

    internal void Unregister(SharedTransitionContent endpoint, SharedTransitionParticipant? participant)
    {
        if (participant is not null) _endpoints.Remove(participant);
    }

    internal void ReportDiagnostic(object? key, string message)
        => _diagnostics.Add(key is null ? message : $"{key}: {message}");

    internal void EnsureCoordinator(Transition parent)
    {
        parent.VerifyAccess();
        if (_coordinator is not null)
        {
            if (!ReferenceEquals(_scope!.ParentTransition, parent))
                throw new InvalidOperationException("A host is bound to one parent Transition.");
            return;
        }
        _diagnostics.Clear();
        _scope = CreateScope(parent);
        _backend = new SharedVisualBackend(this);
        _coordinator = new SharedTransitionCoordinator(_scope, _backend, BoundsSpec);
        _scope.PreparationRequested += OnPreparation;
    }

    private IFiniteAnimationSpec<Rect> BoundsSpec(SharedTransitionPair pair)
    {
        var endpoint = Endpoint(pair.Target) ?? Endpoint(pair.Source);
        return endpoint?.BoundsTransform?.Create(pair.SourceBounds, pair.TargetBounds) ?? new SpringSpec<Rect>();
    }

    private void OnPreparation(SharedTransitionPreparation request)
    {
        _diagnostics.Clear();
        _deadline?.Stop();
        _deadlineGeneration = request.Generation;
        if (PreparationTimeout < TimeSpan.Zero) return;
        _deadline = new DispatcherTimer { Interval = PreparationTimeout };
        _deadline.Tick += (_, _) =>
        {
            _deadline.Stop();
            _coordinator?.ExpirePreparation(_deadlineGeneration);
        };
        _deadline.Start();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_handlingLayout || _coordinator is null) return;
        _handlingLayout = true;
        try
        {
            _diagnostics.Clear();
            _coordinator.LayoutCompleted();
            _backend?.LayoutCompleted();
            if (_coordinator.LastResolution is { } result)
                foreach (var diagnostic in result.Diagnostics)
                    ReportDiagnostic(diagnostic.Key, diagnostic.Reason.ToString());
            if (_scope is { IsPreparing: false }) _deadline?.Stop();
        }
        finally { _handlingLayout = false; }
    }

    private void Teardown()
    {
        _deadline?.Stop();
        _deadline = null;
        if (_scope is not null) _scope.PreparationRequested -= OnPreparation;
        _coordinator?.Dispose();
        _scope?.Dispose();
        _coordinator = null;
        _scope = null;
        _backend = null;
        _endpoints.Clear();
        _overlay?.Clear();
    }

    private static SharedTransitionScope CreateScope(Transition parent)
    {
        for (var type = parent.GetType(); type is not null; type = type.BaseType)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Transition<>)) continue;
            return (SharedTransitionScope)typeof(SharedTransitionHost)
                .GetMethod(nameof(CreateScopeCore), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type.GetGenericArguments()[0])
                .Invoke(null, [parent])!;
        }
        throw new ArgumentException("A typed Transition<TState> is required.", nameof(parent));
    }

    private static SharedTransitionScope CreateScopeCore<TState>(Transition<TState> parent)
        => SharedTransitionScope.Create(parent);
}
