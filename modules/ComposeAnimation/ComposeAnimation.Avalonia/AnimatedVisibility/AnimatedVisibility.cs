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
public class AnimatedVisibility : ContentControl, IEnterExitScopeHost
{
    public static readonly StyledProperty<bool> VisibleProperty =
        AvaloniaProperty.Register<AnimatedVisibility, bool>(nameof(Visible), true);
    public static readonly StyledProperty<VisibilitySource?> SourceProperty =
        AvaloniaProperty.Register<AnimatedVisibility, VisibilitySource?>(nameof(Source));
    public static readonly StyledProperty<EnterTransition> EnterProperty =
        AvaloniaProperty.Register<AnimatedVisibility, EnterTransition>(nameof(Enter),
            new FadeIn() + new ExpandIn(), validate: value => value is not null);
    public static readonly StyledProperty<ExitTransition> ExitProperty =
        AvaloniaProperty.Register<AnimatedVisibility, ExitTransition>(nameof(Exit),
            new FadeOut() + new ShrinkOut(), validate: value => value is not null);
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
    private object? _presentedContent;
    private bool _targetVisible = true;
    private bool _isIdle = true;
    private bool _isContentPresent;
    private AnimatedVisibilityScope? _scope;
    private VisibilityConnection? _connection;
    private IDisposable? _motionConnection;
    private VisibilityLayer? _layer;
    private bool _attached;
    private bool _synchronizing;
    private bool _syncPending;
    private bool _resetPending;
    private bool _failed;

    static AnimatedVisibility()
    {
        HorizontalContentAlignmentProperty.OverrideDefaultValue<AnimatedVisibility>(HorizontalAlignment.Stretch);
        VerticalContentAlignmentProperty.OverrideDefaultValue<AnimatedVisibility>(VerticalAlignment.Stretch);
        TemplateProperty.OverrideDefaultValue<AnimatedVisibility>(new FuncControlTemplate<AnimatedVisibility>((host, names) =>
        {
            var presenter = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~ContentProperty] = new TemplateBinding(PresentedContentProperty),
                [~ContentTemplateProperty] = new TemplateBinding(ContentTemplateProperty),
                [~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
                [~BorderBrushProperty] = new TemplateBinding(BorderBrushProperty),
                [~BorderThicknessProperty] = new TemplateBinding(BorderThicknessProperty),
                [~CornerRadiusProperty] = new TemplateBinding(CornerRadiusProperty),
                [~PaddingProperty] = new TemplateBinding(PaddingProperty),
                [~HorizontalContentAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty),
                [~VerticalContentAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty)
            };
            names.Register(presenter.Name, presenter);
            var layer = new VisibilityLayer { Name = "PART_VisibilityLayer" };
            layer.Surface.Child = presenter;
            names.Register(layer.Name, layer);
            return layer;
        }));
    }

    public bool Visible { get => GetValue(VisibleProperty); set => SetValue(VisibleProperty, value); }
    public VisibilitySource? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public EnterTransition Enter { get => GetValue(EnterProperty); set => SetValue(EnterProperty, value); }
    public ExitTransition Exit { get => GetValue(ExitProperty); set => SetValue(ExitProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public bool TargetVisible => _targetVisible;
    public bool IsIdle => _isIdle;
    public bool IsContentPresent => _isContentPresent;
    public AnimatedVisibilityScope? Scope => _scope;
    public event EventHandler<VisibilityScopeEventArgs>? ScopeCreated;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _failed = false;
        Synchronize();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        Synchronize(reset: true);
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _layer?.ResetMotion();
        base.OnApplyTemplate(e);
        _layer = e.NameScope.Find<VisibilityLayer>("PART_VisibilityLayer")
            ?? throw new InvalidOperationException("AnimatedVisibility requires its default visibility template.");
        _layer.ResolveScope = () => Scope;
        _layer.Enter = () => Enter;
        _layer.Exit = () => Exit;
        _layer.Label = () => Label;
        _layer.Measured = () => Scope?.Measured();
        _layer.IsVisible = IsContentPresent;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisibleProperty)
        {
            if (Source is null && _connection is not null) _connection.Transition.UpdateTarget(Visible);
            Synchronize();
        }
        else if (change.Property == SourceProperty || change.Property == ContentProperty ||
                 change.Property == ContentTemplateProperty || change.Property == IsVisibleProperty)
        {
            if (change.Property == SourceProperty) _failed = false;
            Synchronize(reset: true);
        }
    }

    private void OnPhaseChanged() => Synchronize();

    private void Synchronize(bool reset = false)
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
                    Disconnect();
                }
                if (!_attached || !IsVisible || _failed || AnimationScope.IsEnvironmentEnding(this))
                {
                    SetAndRaise(IsIdleProperty, ref _isIdle, true);
                    continue;
                }
                if (_connection is null)
                {
                    var motion = AnimationScope.Get(this);
                    var clock = motion.Clock;
                    _connection = Source?.Connect(clock, Label)
                        ?? new VisibilityConnection(new Transition<bool>(Visible, clock, Label), true);
                    _connection.Transition.PhaseChanged += OnPhaseChanged;
                    _motionConnection = motion.RegisterResource(() =>
                    {
                        if (!motion.IsEnding) return;
                        _failed = true;
                        Disconnect();
                    });
                    if (_connection.OwnsLoop)
                    {
                        // Queue loop startup so registration and the first layout can finish first.
                        var connection = _connection;
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (!ReferenceEquals(_connection, connection)) return;
                            // Keep the layout barrier, but let awaits in the frame loop capture
                            // Normal rather than inheriting Loaded for the loop's lifetime.
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (ReferenceEquals(_connection, connection) && !motion.IsEnding)
                                    _ = motion.Launch(ct => RunAsync(connection, ct), Label ?? "Visibility");
                            }, DispatcherPriority.Normal);
                        }, DispatcherPriority.Loaded);
                    }
                }
                var parent = _connection.Transition;
                if (parent.IsDisposed)
                {
                    Disconnect();
                    _failed = true;
                    continue;
                }
                SetAndRaise(TargetVisibleProperty, ref _targetVisible, parent.TargetState);
                if (Scope is null && (parent.CurrentState || parent.TargetState || parent.IsSeeking))
                    CreateScope(parent);

                var scope = Scope;
                if (scope is not null && !scope.IsDisposed)
                {
                    var transition = scope.Transition;
                    var idle = scope.IsReady && !transition.IsPreparing && !transition.IsRunning &&
                        transition.CurrentState == transition.TargetState &&
                        transition.Animations.All(x => x.IsFinished) &&
                        transition.Transitions.All(IsFinished);
                    // Seeking is a preview; keep the content even at the hidden endpoint.
                    if (idle && !parent.TargetState && transition.TargetState == EnterExitState.PostExit && !parent.IsSeeking)
                        EndScope();
                    SetAndRaise(IsIdleProperty, ref _isIdle, idle && !parent.IsSeeking);
                }
                else SetAndRaise(IsIdleProperty, ref _isIdle, true);
            }
        }
        catch (Exception error)
        {
            _failed = true;
            try { Disconnect(); }
            finally { AnimationScope.ReportError(this, error); }
            throw;
        }
        finally { _synchronizing = false; }
    }

    private static bool IsFinished(Transition transition)
        => !transition.IsPreparing && !transition.IsRunning && transition.Animations.All(x => x.IsFinished) && transition.Transitions.All(IsFinished);

    private void CreateScope(Transition<bool> parent)
    {
        var mapper = new EnterExitStateMapper<bool>(parent, static v => v, HasBeenVisibleMode.Argument);
        var scope = new AnimatedVisibilityScope(parent.CreateChildTransition(mapper.Map, "EnterExit"));
        SetAndRaise(ScopeProperty, ref _scope, scope);
        scope.Transition.PhaseChanged += OnPhaseChanged;
        SetAndRaise(IsContentPresentProperty, ref _isContentPresent, true);
        SetAndRaise(PresentedContentProperty, ref _presentedContent, Content);
        ApplyTemplate();
        if (_layer is not null)
        {
            _layer.IsVisible = true;
            _layer.InvalidateMeasure();
        }
        InvalidateMeasure();
        ScopeCreated?.Invoke(this, new VisibilityScopeEventArgs(scope));
    }

    private void EndScope()
    {
        var scope = _scope;
        if (scope is null) return;
        scope.Transition.PhaseChanged -= OnPhaseChanged;
        SetAndRaise(ScopeProperty, ref _scope, null);
        try
        {
            _layer?.ResetMotion();
            scope.Dispose();
        }
        finally
        {
            SetAndRaise(IsContentPresentProperty, ref _isContentPresent, false);
            SetAndRaise(IsIdleProperty, ref _isIdle, true);
            SetAndRaise(PresentedContentProperty, ref _presentedContent, null);
            if (_layer is not null) _layer.IsVisible = false;
            InvalidateMeasure();
        }
    }

    private void Disconnect()
    {
        _motionConnection?.Dispose();
        _motionConnection = null;
        var connection = _connection;
        _connection = null;
        if (connection is not null) connection.Transition.PhaseChanged -= OnPhaseChanged;
        try { EndScope(); }
        finally { connection?.Dispose(); }
    }

    private async Task RunAsync(VisibilityConnection connection, CancellationToken cancellationToken)
    {
        try { await connection.Transition.RunAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            if (ReferenceEquals(_connection, connection))
            {
                _failed = true;
                Synchronize(reset: true);
            }
            throw;
        }
    }

    // Returning zero from MeasureOverride alone still includes Margin and MinWidth/MinHeight.
    protected override Size MeasureCore(Size availableSize)
    {
        try { return IsContentPresent ? base.MeasureCore(availableSize) : default; }
        catch (Exception error)
        {
            _failed = true;
            Synchronize(reset: true);
            AnimationScope.ReportError(this, error);
            throw;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
        => IsContentPresent ? base.ArrangeOverride(finalSize) : default;
}
