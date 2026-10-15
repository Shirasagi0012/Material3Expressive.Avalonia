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
/// Presents live content until every animation in its visibility scope finishes exiting.
/// Bind Visible for animated visibility; native IsVisible still hides immediately.
/// </summary>
public partial class AnimatedVisibility : ContentControl, IEnterExitScopeHost, IAnimationScopeHost,
    ILookaheadLayoutPolicy, ILookaheadPreparationParticipant
{
    public static readonly StyledProperty<bool> VisibleProperty =
        AvaloniaProperty.Register<AnimatedVisibility, bool>(nameof(Visible), true);

    public static readonly StyledProperty<VisibilitySource?> SourceProperty =
        AvaloniaProperty.Register<AnimatedVisibility, VisibilitySource?>(nameof(Source));

    public static readonly StyledProperty<EnterTransition> EnterProperty =
        AvaloniaProperty.Register<AnimatedVisibility, EnterTransition>(nameof(Enter),
            new FadeIn() + new ExpandIn(), validate: value => value is { });

    public static readonly StyledProperty<ExitTransition> ExitProperty =
        AvaloniaProperty.Register<AnimatedVisibility, ExitTransition>(nameof(Exit),
            new FadeOut() + new ShrinkOut(), validate: value => value is { });

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AnimatedVisibility, string?>(nameof(Label));

    public static readonly DirectProperty<AnimatedVisibility, bool> TargetVisibleProperty =
        AvaloniaProperty.RegisterDirect<AnimatedVisibility, bool>(nameof(TargetVisible), x => x.TargetVisible);

    public static readonly DirectProperty<AnimatedVisibility, bool> IsIdleProperty =
        AvaloniaProperty.RegisterDirect<AnimatedVisibility, bool>(nameof(IsIdle), x => x.IsIdle);

    public static readonly DirectProperty<AnimatedVisibility, bool> IsContentPresentProperty =
        AvaloniaProperty.RegisterDirect<AnimatedVisibility, bool>(nameof(IsContentPresent), x => x.IsContentPresent);

    public static readonly DirectProperty<AnimatedVisibility, AnimatedVisibilityScope?> ScopeProperty =
        AvaloniaProperty.RegisterDirect<AnimatedVisibility, AnimatedVisibilityScope?>(nameof(Scope), x => x.Scope);

    private static readonly DirectProperty<AnimatedVisibility, object?> PresentedContentProperty =
        AvaloniaProperty.RegisterDirect<AnimatedVisibility, object?>("PresentedContent", x => x._presentedContent);

    private static readonly DirectProperty<AnimatedVisibility, IDataTemplate?> PresentedContentTemplateProperty =
        AvaloniaProperty.RegisterDirect<AnimatedVisibility, IDataTemplate?>("PresentedContentTemplate",
            x => x._presentedContentTemplate);

    private object? _presentedContent;
    private IDataTemplate? _presentedContentTemplate;
    private bool _targetVisible = true;
    private bool _isIdle = true;
    private bool _isContentPresent;
    private AnimatedVisibilityScope? _visibilityScope;
    private VisibilityConnection? _visibilityConnection;
    private readonly ScopeResumeBinding _animationScopeBinding;

    public AnimatedVisibility()
    {
        LookaheadLayout.RegisterInherited(this);
        _animationScopeBinding = new ScopeResumeBinding(this, () => _attached, () => {
            _failed = true;
            DisconnectVisibilitySource();
        });
    }

    // One launch path for the first start (deferred) and a D17 resume (immediate).
    private void StartLoop(VisibilityConnection connection, AnimationScope animationScope, bool immediate)
        => ScopeResumeBinding.StartLoop(animationScope, () => ReferenceEquals(_visibilityConnection, connection),
            ct => RunAsync(connection, animationScope, ct), Label ?? "Visibility", immediate);

    private EnterExitLayout? _enterExitLayout;
    private EnterExitContentPresenter? _contentPresenter;
    private EnterExitMotion? _enterExitMotion;
    private bool _attached;
    private bool _synchronizing;
    private bool _syncPending;
    private bool _resetPending;
    private bool _failed;

    static AnimatedVisibility()
    {
        HorizontalContentAlignmentProperty.OverrideDefaultValue<AnimatedVisibility>(HorizontalAlignment.Stretch);
        VerticalContentAlignmentProperty.OverrideDefaultValue<AnimatedVisibility>(VerticalAlignment.Stretch);
        TemplateProperty.OverrideDefaultValue<AnimatedVisibility>(new FuncControlTemplate<AnimatedVisibility>((host, names) => {
            var presenter = new EnterExitContentPresenter {
                Name = "PART_ContentPresenter", [~ContentProperty] = new TemplateBinding(PresentedContentProperty),
                [~ContentTemplateProperty] = new TemplateBinding(PresentedContentTemplateProperty),
                [~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
                [~BorderBrushProperty] = new TemplateBinding(BorderBrushProperty),
                [~BorderThicknessProperty] = new TemplateBinding(BorderThicknessProperty),
                [~CornerRadiusProperty] = new TemplateBinding(CornerRadiusProperty),
                [~PaddingProperty] = new TemplateBinding(PaddingProperty),
                [~HorizontalContentAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty),
                [~VerticalContentAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty)
            };
            names.Register(presenter.Name, presenter);

            var layer = new EnterExitLayout { Name = "PART_EnterExitLayout", Surface = { Child = presenter } };
            names.Register(layer.Name, layer);

            return layer;
        }));
    }

    public bool Visible
    {
        get => GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }

    public VisibilitySource? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public EnterTransition Enter
    {
        get => GetValue(EnterProperty);
        set => SetValue(EnterProperty, value);
    }

    public ExitTransition Exit
    {
        get => GetValue(ExitProperty);
        set => SetValue(ExitProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool TargetVisible => _targetVisible;
    public bool IsIdle => _isIdle;
    public bool IsContentPresent => _isContentPresent;
    public AnimatedVisibilityScope? Scope => _visibilityScope;
    public event EventHandler<VisibilityScopeEventArgs>? ScopeCreated;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _failed = false;
        SynchronizePresentation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        SynchronizePresentation();
        LookaheadLayout.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _enterExitLayout?.SetMotion(null);
        base.OnApplyTemplate(e);
        _enterExitLayout = e.NameScope.Find<EnterExitLayout>("PART_EnterExitLayout")
         ?? throw new InvalidOperationException("AnimatedVisibility requires its enter/exit layout template part.");
        _contentPresenter = e.NameScope.Find<EnterExitContentPresenter>("PART_ContentPresenter")
         ?? throw new InvalidOperationException("AnimatedVisibility requires its cooperative content presenter.");
        _enterExitLayout.SetMotion(_enterExitMotion);
        _enterExitLayout.IsVisible = IsContentPresent;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VisualParentProperty)
            LookaheadLayout.VisualParentChanged(this);

        if (change.Property == EnterProperty || change.Property == ExitProperty)
            _enterExitMotion?.UpdateTransitions(Enter, Exit);

        if (change.Property == VisibleProperty)
        {
            if (Source is null && _visibilityConnection is { }) _visibilityConnection.Transition.UpdateTarget(Visible);
            SynchronizePresentation();
        }
        else if (change.Property == SourceProperty
              || change.Property == ContentProperty
              || change.Property == ContentTemplateProperty
              || change.Property == IsVisibleProperty)
        {
            if (change.Property == SourceProperty) _failed = false;

            SynchronizePresentation(true);
        }
    }

    // a rebuild while attached keeps the connection and its transition state (the resource
    // callback only cancelled the run); re-run the same transition in the replacement scope. The
    // drain runs before the frame's waiter snapshot, so the run samples this frame, continuing
    // from its stored play time. Detach is handled by OnDetachedFromVisualTree.
    void IAnimationScopeHost.OnScopeChanged(AnimationScopeChangedEventArgs e)
        => _animationScopeBinding.Handle(e,
            onResume: animationScope => {
                if (_visibilityConnection is not { } connection || connection.Transition.IsDisposed) return false;
                _failed = false;
                // A re-entrant Get from a ScopeChanged handler that runs it again simply takes over.
                if (connection.RequiresFrameLoop) StartLoop(connection, animationScope, true);
                return true;
            },
            onLost: () => {
                _failed = true;
                DisconnectVisibilitySource();
            },
            // Intentional: a scope change without a resume resets the presentation (reset: true).
            resumed => {
                _failed = false;
                SynchronizePresentation(!resumed);
            });

    private void OnPhaseChanged() => SynchronizePresentation();

    private void SynchronizePresentation(bool reset = false)
    {
        _syncPending = true;
        _resetPending |= reset;
        if (_synchronizing) return;
        _synchronizing = true;
        try
        {
            while (_syncPending)
            {
                _syncPending = false;
                if (Source is null) SetAndRaise(TargetVisibleProperty, ref _targetVisible, Visible);
                if (_resetPending)
                {
                    _resetPending = false;
                    DisconnectVisibilitySource();
                }

                if (!_attached || !IsVisible || _failed || AnimationScope.IsEnvironmentEnding(this))
                {
                    SetAndRaise(IsIdleProperty, ref _isIdle, true);
                    continue;
                }

                if (_visibilityConnection is null)
                {
                    var animationScope = AnimationScope.Get(this);

                    _visibilityConnection = Source?.Connect(Label)
                     ?? new VisibilityConnection(new Transition<bool>(Visible, Label), true);
                    _visibilityConnection.Transition.PhaseChanged += OnPhaseChanged;

                    _animationScopeBinding.Bind(animationScope);

                    if (_visibilityConnection.RequiresFrameLoop)
                        StartLoop(_visibilityConnection, animationScope, false);
                }

                Transition<bool> parent = _visibilityConnection.Transition;
                if (parent.IsDisposed)
                {
                    DisconnectVisibilitySource();
                    _failed = true;
                    continue;
                }

                SetAndRaise(TargetVisibleProperty, ref _targetVisible, parent.TargetState);

                if (Scope is null && (parent.CurrentState
                     || parent.TargetState
                     || parent.IsSeeking
                     || parent is { HasPendingTargetState: true, PendingTargetState: true }))
                    CreatePresentation(parent);

                AnimatedVisibilityScope? scope = Scope;
                if (scope is { IsDisposed: false })
                {
                    Transition<EnterExitState> transition = scope.Transition;
                    bool idle = !parent.HasPendingTargetState
                     && scope.IsInitialLayoutReady
                     && transition is { IsPreparing: false, IsRunning: false }
                     && transition.CurrentState == transition.TargetState
                     && transition.Animations.All(x => x.IsFinished)
                     && transition.Transitions.All(IsFinished);

                    // Seeking is a preview; keep the content even at the hidden endpoint.
                    if (idle && !parent.TargetState && transition.TargetState == EnterExitState.PostExit && !parent.IsSeeking)
                        EndPresentation();
                    SetAndRaise(IsIdleProperty, ref _isIdle, idle && !parent.IsSeeking);
                }
                else SetAndRaise(IsIdleProperty, ref _isIdle, true);
            }
        }
        catch (Exception error)
        {
            _failed = true;
            try
            {
                DisconnectVisibilitySource();
            }
            finally
            {
                AnimationScope.ReportError(this, error);
            }

            throw;
        }
        finally
        {
            _synchronizing = false;
        }
    }

    private static bool IsFinished(Transition transition)
        => transition is { IsPreparing: false, IsRunning: false }
         && transition.Animations.All(x => x.IsFinished)
         && transition.Transitions.All(IsFinished);

    private void CreatePresentation(Transition<bool> parent)
    {
        var mapper = new EnterExitStateMapper<bool>(parent, static v => v);
        var scope = new AnimatedVisibilityScope(parent.CreateChildTransition(mapper.Map, "EnterExit"));

        SetAndRaise(ScopeProperty, ref _visibilityScope, scope);

        scope.Transition.PhaseChanged += OnPhaseChanged;
        _enterExitMotion = new EnterExitMotion(scope, Label);
        _enterExitMotion.UpdateTransitions(Enter, Exit);

        SetAndRaise(IsContentPresentProperty, ref _isContentPresent, true);
        SetAndRaise(PresentedContentProperty, ref _presentedContent, Content);
        SetAndRaise(PresentedContentTemplateProperty, ref _presentedContentTemplate, ContentTemplate);
        ApplyTemplate();

        if (_enterExitLayout is { })
        {
            _enterExitLayout.SetMotion(_enterExitMotion);
            _enterExitLayout.IsVisible = true;
            _enterExitLayout.InvalidateMeasure();
        }

        InvalidateMeasure();
        ScopeCreated?.Invoke(this, new VisibilityScopeEventArgs(scope));
    }

    private void EndPresentation()
    {
        AnimatedVisibilityScope? scope = _visibilityScope;
        if (scope is null) return;
        scope.Transition.PhaseChanged -= OnPhaseChanged;
        SetAndRaise(ScopeProperty, ref _visibilityScope, null);
        try
        {
            _enterExitLayout?.SetMotion(null);
            _enterExitMotion?.Dispose();
            _enterExitMotion = null;
            scope.Dispose();
        }
        finally
        {
            SetAndRaise(IsContentPresentProperty, ref _isContentPresent, false);
            SetAndRaise(IsIdleProperty, ref _isIdle, true);
            SetAndRaise(PresentedContentTemplateProperty, ref _presentedContentTemplate, null);
            SetAndRaise(PresentedContentProperty, ref _presentedContent, null);

            _enterExitLayout?.IsVisible = false;
            InvalidateMeasure();
        }
    }

    private void DisconnectVisibilitySource()
    {
        _animationScopeBinding.Reset();
        VisibilityConnection? connection = _visibilityConnection;
        _visibilityConnection = null;
        if (connection is { }) connection.Transition.PhaseChanged -= OnPhaseChanged;
        try
        {
            EndPresentation();
        }
        finally
        {
            connection?.Dispose();
        }
    }

    private async Task RunAsync(VisibilityConnection connection, FrameScope scope, CancellationToken cancellationToken)
    {
        try
        {
            await connection.Transition.RunAsync(scope, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (ReferenceEquals(_visibilityConnection, connection))
            {
                _failed = true;
                SynchronizePresentation(true);
            }

            throw;
        }
    }
}
