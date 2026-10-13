using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Presents overlapping enter/exit content when <see cref="ContentControl.Content"/> changes.
/// Bind Content as the target state; each live slot freezes that value and the current template.
/// </summary>
public class AnimatedContent : ContentControl, IAnimationScopeHost
{
    public static readonly StyledProperty<ContentSource?> SourceProperty =
        AvaloniaProperty.Register<AnimatedContent, ContentSource?>(nameof(Source));
    public static readonly StyledProperty<ContentTransform?> ContentTransformProperty =
        AvaloniaProperty.Register<AnimatedContent, ContentTransform?>(nameof(ContentTransform));
    public static readonly StyledProperty<VisibilityAlignment> ContentAlignmentProperty =
        AvaloniaProperty.Register<AnimatedContent, VisibilityAlignment>(nameof(ContentAlignment),
            VisibilityAlignment.TopStart);
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AnimatedContent, string?>(nameof(Label));
    public static readonly DirectProperty<AnimatedContent, object?> TargetContentProperty =
        AvaloniaProperty.RegisterDirect<AnimatedContent, object?>(nameof(TargetContent), x => x.TargetContent);
    public static readonly DirectProperty<AnimatedContent, bool> IsIdleProperty =
        AvaloniaProperty.RegisterDirect<AnimatedContent, bool>(nameof(IsIdle), x => x.IsIdle);
    public static readonly DirectProperty<AnimatedContent, int> VisibleSlotCountProperty =
        AvaloniaProperty.RegisterDirect<AnimatedContent, int>(nameof(VisibleSlotCount), x => x.VisibleSlotCount);
    public static readonly DirectProperty<AnimatedContent, AnimatedVisibilityScope?> TargetScopeProperty =
        AvaloniaProperty.RegisterDirect<AnimatedContent, AnimatedVisibilityScope?>(nameof(TargetScope), x => x.TargetScope);

    private readonly List<ContentSlot> _currentlyVisible = [];
    private ContentSlotPanel? _panel;
    private Border? _sizeBox;
    private ContentConnection? _connection;
    private readonly ScopeResumeBinding _scopeBinding;

    public AnimatedContent()
    {
        _scopeBinding = new ScopeResumeBinding(this, () => _attached, () =>
        {
            _failed = true;
            Disconnect();
        });
    }

    // One launch path for the first start (deferred) and a D17 resume (immediate).
    private void StartLoop(ContentConnection connection, AnimationScope motion, bool immediate)
        => ScopeResumeBinding.StartLoop(motion, () => ReferenceEquals(_connection, connection),
            ct => RunAsync(connection, motion, ct), Label ?? "AnimatedContent", immediate);
    private ContentTransform? _fallbackTransform;
    private ContentTransform? _resolvedTransform;
    private object? _targetContent;
    private bool _isIdle = true;
    private int _visibleSlotCount;
    private AnimatedVisibilityScope? _targetScope;
    private bool _attached;
    private bool _synchronizing;
    private bool _syncPending;
    private bool _resetPending;
    private bool _failed;
    private object? _lastContent;

    static AnimatedContent()
    {
        HorizontalContentAlignmentProperty.OverrideDefaultValue<AnimatedContent>(Avalonia.Layout.HorizontalAlignment.Stretch);
        VerticalContentAlignmentProperty.OverrideDefaultValue<AnimatedContent>(Avalonia.Layout.VerticalAlignment.Stretch);
        TemplateProperty.OverrideDefaultValue<AnimatedContent>(new FuncControlTemplate<AnimatedContent>((host, names) =>
        {
            var panel = new ContentSlotPanel { Host = host };
            var box = new Border
            {
                Name = "PART_ContentPanel",
                Child = panel,
                [~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
                [~BorderBrushProperty] = new TemplateBinding(BorderBrushProperty),
                [~BorderThicknessProperty] = new TemplateBinding(BorderThicknessProperty),
                [~CornerRadiusProperty] = new TemplateBinding(CornerRadiusProperty),
                [~PaddingProperty] = new TemplateBinding(PaddingProperty)
            };
            names.Register(box.Name, box);
            var viewport = new ContentViewport { Name = "PART_Viewport", Host = host, Child = box };
            names.Register(viewport.Name, viewport);
            return viewport;
        }));
    }

    public ContentSource? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public ContentTransform? ContentTransform
    {
        get => GetValue(ContentTransformProperty);
        set => SetValue(ContentTransformProperty, value);
    }
    public Func<IAnimatedContentTransitionScope, ContentTransform>? TransitionSpec { get; set; }
    public Func<object?, object?>? ContentKeySelector { get; set; }
    public VisibilityAlignment ContentAlignment
    {
        get => GetValue(ContentAlignmentProperty);
        set => SetValue(ContentAlignmentProperty, value);
    }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public object? TargetContent => _targetContent;
    public bool IsIdle => _isIdle;
    public int VisibleSlotCount => _visibleSlotCount;
    public AnimatedVisibilityScope? TargetScope => _targetScope;
    public event EventHandler<ContentSlotEventArgs>? SlotCreated;

    internal ContentConnection? Connection => _connection;
    internal ContentTransform? ResolvedTransform => _resolvedTransform;
    internal Size CurrentSize { get; set; }
    internal Size TargetContentSize { get; set; }

    protected override bool RegisterContentPresenter(ContentPresenter presenter) => false;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (e.NameScope.Find<ContentPresenter>("PART_ContentPresenter") is not null)
            throw new InvalidOperationException("AnimatedContent rejects PART_ContentPresenter; content is presented in slots.");
        _sizeBox = e.NameScope.Find<Border>("PART_ContentPanel")
            ?? throw new InvalidOperationException("AnimatedContent requires PART_ContentPanel.");
        _panel = _sizeBox.Child as ContentSlotPanel
            ?? throw new InvalidOperationException("AnimatedContent requires PART_ContentPanel with a ContentSlotPanel child.");
        _panel.Host = this;
        if (e.NameScope.Find<ContentViewport>("PART_Viewport") is { } viewport)
            viewport.Host = this;
    }

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
        Synchronize(); // No reset (D17): see ScopeResumeBinding.
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ContentProperty && change.NewValue is ILogical incoming &&
            incoming.LogicalParent is not null && incoming.LogicalParent != this)
        {
            PseudoClasses.Set(":empty", Content is null);
            HandleContentChange(change.GetOldValue<object?>(), Content);
            return;
        }

        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
        {
            if (change.NewValue is ILogical logical)
                LogicalChildren.Remove(logical);
            HandleContentChange(change.GetOldValue<object?>(), Content);
        }
        else if (change.Property == SourceProperty || change.Property == IsVisibleProperty)
        {
            if (change.Property == SourceProperty) _failed = false;
            Synchronize(reset: true);
        }
        else if (change.Property == ContentTemplateProperty)
        {
            var target = TargetSlot();
            if (target is not null)
            {
                target.FrozenTemplate = ContentTemplate;
                target.Presenter.ContentTemplate = ContentTemplate;
                target.Presenter.UpdateChild();
            }
        }
        else if (change.Property == ContentTransformProperty)
        {
            _fallbackTransform = null;
            if (_connection is not null) Synchronize();
        }
        else if (change.Property == ContentAlignmentProperty)
            InvalidateMeasure();
    }

    private void HandleContentChange(object? old, object? next)
    {
        _lastContent = next;
        if (!_attached) return;
        if (Source is null && _connection is not null &&
            !KeysEqual(_connection.Transition.TargetState, next))
        {
            try
            {
                ApplyOutgoingTransforms(next);
                _connection.Transition.UpdateTarget(next);
            }
            catch (Exception error)
            {
                _failed = true;
                AnimationScope.ReportError(this, error);
                throw;
            }
        }
        Synchronize();
    }

    // D17: a rebuild while attached keeps the connection, its slots and transition state (the
    // resource callback only cancelled the run); re-run the same loop in the replacement scope.
    // The drain runs before the frame's waiter snapshot, so the run samples this frame,
    // continuing from its stored play time. Detach is handled by OnDetachedFromVisualTree.
    void IAnimationScopeHost.OnScopeChanged(AnimationScopeChangedEventArgs e)
        => _scopeBinding.Handle(e,
            onResume: motion =>
            {
                if (_connection is not { } connection || connection.Transition.IsDisposed) return false;
                _failed = false;
                // A re-entrant Get from a ScopeChanged handler that runs it again simply takes over.
                if (connection.OwnsLoop) StartLoop(connection, motion, immediate: true);
                return true;
            },
            onLost: () =>
            {
                _failed = true;
                Disconnect();
            },
            // Intentional: a scope change without a resume resets the presentation (reset: true).
            invalidate: resumed =>
            {
                _failed = false;
                Synchronize(reset: !resumed);
            });

    internal void OnPhaseChanged() => Synchronize();

    private void OnParentTargetChanging()
    {
        if (_connection is null) return;
        ApplyOutgoingTransforms(_connection.Transition.ChangingTarget);
    }

    private void ApplyOutgoingTransforms(object? upcoming)
    {
        if (_connection is null) return;
        var parent = _connection.Transition;
        var scope = new AnimatedContentTransitionScope(this, parent.TargetState, upcoming);
        var transform = ResolveCurrentTransform(scope);
        _resolvedTransform = transform;
        foreach (var slot in _currentlyVisible)
        {
            if (KeysEqual(slot.State, upcoming))
            {
                slot.Enter = transform.Enter;
                slot.ApplyZIndex(transform.TargetContentZIndex);
            }
            else slot.Exit = transform.Exit;
        }
    }

    protected virtual ContentTransform ResolveCurrentTransform(IAnimatedContentTransitionScope scope)
    {
        if (TransitionSpec is { } spec) return spec(scope);
        if (ContentTransform is { } transform) return transform;
        return _fallbackTransform ??= ContentTransform.CreateFadeThrough();
    }

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
                ApplyTemplate();
                if (_panel is null)
                    throw new InvalidOperationException("AnimatedContent requires PART_ContentPanel with a ContentSlotPanel child.");
                if (_connection is null)
                {
                    var motion = AnimationScope.Get(this);
                    var label = Label ?? "AnimatedContent";
                    _connection = Source?.Connect(label)
                        ?? new ContentConnection(new Transition<object?>(Content, label), true);
                    _connection.Transition.PhaseChanged += OnPhaseChanged;
                    _connection.Transition.TargetChanging += OnParentTargetChanging;
                    _scopeBinding.Bind(motion);
                    _lastContent ??= Content;
                    if (_connection.OwnsLoop) StartLoop(_connection, motion, immediate: false);
                }
                var parent = _connection.Transition;
                if (parent.IsDisposed)
                {
                    Disconnect();
                    _failed = true;
                    continue;
                }
                if (Source is null && !parent.IsSeeking && !KeysEqual(parent.TargetState, Content))
                    parent.UpdateTarget(Content);
                if (Source is null && SlotFor(Content) is { } live && !ReferenceEquals(live.State, Content))
                {
                    live.ReplaceState(Content, KeyOf(Content));
                    _panel?.ClearLastSize();
                }
                SetAndRaise(TargetContentProperty, ref _targetContent, parent.TargetState);
                _resolvedTransform ??= ResolveCurrentTransform(new AnimatedContentTransitionScope(
                    this, parent.CurrentState, parent.TargetState));
                ReconcileSlots();
                InvalidateMeasure();
                EvaluateSlotDisposal();
                PublishStatus();
            }
        }
        catch (Exception error)
        {
            _failed = true;
            try { Disconnect(); }
            finally
            {
                AnimationScope.ReportError(this, error);
            }
            throw;
        }
        finally { _synchronizing = false; }
    }

    private void ReconcileSlots()
    {
        var parent = _connection!.Transition;
        if (parent.IsSeeking && SlotFor(parent.CurrentState) is null)
        {
            foreach (var slot in _currentlyVisible.ToArray())
            {
                if (!KeysEqual(slot.State, parent.CurrentState)) DisposeSlot(slot);
            }
            if (SlotFor(parent.CurrentState) is null) CreateSlot(parent.CurrentState);
        }

        if (!parent.IsSeeking && EqualityComparer<object?>.Default.Equals(parent.CurrentState, parent.TargetState))
        {
            if (SlotFor(parent.CurrentState) is null) CreateSlot(parent.CurrentState);
        }

        var target = parent.TargetState;
        var id = IndexOfKey(target);
        if (id < 0)
        {
            CreateSlot(target);
            id = _currentlyVisible.Count - 1;
        }
        else
        {
            if (!ReferenceEquals(_currentlyVisible[id].State, target))
                _currentlyVisible[id].ReplaceState(target, KeyOf(target));
            if (id != _currentlyVisible.Count - 1)
            {
                var slot = _currentlyVisible[id];
                _currentlyVisible.RemoveAt(id);
                _currentlyVisible.Add(slot);
                var visualIndex = _panel!.Children.IndexOf(slot);
                if (visualIndex >= 0 && visualIndex != _panel.Children.Count - 1)
                    _panel.Children.Move(visualIndex, _panel.Children.Count - 1);
            }
        }

        for (var i = 0; i < _currentlyVisible.Count; i++)
            _currentlyVisible[i].IsTarget = i == _currentlyVisible.Count - 1;
        var incoming = _currentlyVisible[^1];
        incoming.ApplyZIndex(_resolvedTransform?.TargetContentZIndex ?? 0f);
    }

    private void CreateSlot(object? state)
    {
        if (state is Control control && control.Parent is not null &&
            control.GetVisualAncestors().OfType<ContentSlot>().Any(s => s.Host == this && !ReferenceEquals(s.State, state)))
            throw new InvalidOperationException("A Control cannot be presented in two AnimatedContent slots.");

        var parent = _connection!.Transition;
        var slot = new ContentSlot(this, state, KeyOf(state));
        var mapper = new EnterExitStateMapper<object?>(parent, s => KeysEqual(s, slot.State), HasBeenVisibleMode.ParentCurrent);
        slot.Mapper = mapper;
        var child = parent.CreateChildTransition(mapper.Map, "EnterExit");
        slot.AttachScope(new AnimatedVisibilityScope(child));
        slot.FrozenTemplate = ContentTemplate;
        slot.Presenter.ContentTemplate = slot.FrozenTemplate;
        slot.Presenter.Content = state;
        _panel!.Children.Add(slot);
        _currentlyVisible.Add(slot);
        slot.Presenter.UpdateChild();
        var staying = KeysEqual(state, parent.TargetState);
        var transform = _resolvedTransform ?? ContentTransform.CreateFadeThrough();
        if (staying)
        {
            slot.Enter = transform.Enter;
            slot.Exit = ExitTransition.None;
            slot.ApplyZIndex(transform.TargetContentZIndex);
        }
        else
        {
            slot.Enter = EnterTransition.None;
            slot.Exit = transform.Exit;
        }
        SlotCreated?.Invoke(this, new ContentSlotEventArgs(slot.Scope!, state));
    }

    private void EvaluateSlotDisposal()
    {
        var parent = _connection?.Transition;
        if (parent is null) return;
        foreach (var slot in _currentlyVisible.ToArray())
        {
            if (parent.IsSeeking) continue;
            if (KeysEqual(slot.State, parent.TargetState) &&
                EqualityComparer<object?>.Default.Equals(parent.CurrentState, parent.TargetState))
                continue;
            var scope = slot.Scope;
            if (scope is null || scope.IsDisposed || !scope.IsReady) continue;
            var child = scope.Transition;
            var childIdle = !child.IsPreparing && !child.IsRunning &&
                child.CurrentState == EnterExitState.PostExit &&
                child.TargetState == EnterExitState.PostExit &&
                child.Animations.All(x => x.IsFinished) &&
                child.Transitions.All(IsFinished);
            if (!childIdle) continue;
            if (!slot.Hold) DisposeSlot(slot);
            else if (EqualityComparer<object?>.Default.Equals(parent.CurrentState, parent.TargetState) &&
                     ParentTreeIdle(parent))
                DisposeSlot(slot);
        }
    }

    private void DisposeSlot(ContentSlot slot)
    {
        slot.Detach();
        _currentlyVisible.Remove(slot);
        _panel?.Children.Remove(slot);
    }

    private void PublishStatus()
    {
        var parent = _connection?.Transition;
        var target = TargetSlot();
        SetAndRaise(VisibleSlotCountProperty, ref _visibleSlotCount, _currentlyVisible.Count);
        SetAndRaise(TargetScopeProperty, ref _targetScope, target?.Scope);
        var idle = parent is not null &&
            !parent.IsSeeking && !parent.IsPreparing && !parent.IsRunning &&
            EqualityComparer<object?>.Default.Equals(parent.CurrentState, parent.TargetState) &&
            parent.Animations.All(x => x.IsFinished) && parent.Transitions.All(IsFinished) &&
            _currentlyVisible.Count <= 1 &&
            (target?.Scope is not { } scope || (scope.IsReady && !scope.Transition.IsRunning &&
                scope.Transition.CurrentState == scope.Transition.TargetState &&
                scope.Transition.Animations.All(x => x.IsFinished)));
        SetAndRaise(IsIdleProperty, ref _isIdle, idle);
    }

    private static bool IsFinished(Transition transition)
        => !transition.IsPreparing && !transition.IsRunning && transition.Animations.All(x => x.IsFinished) &&
           transition.Transitions.All(IsFinished);

    private static bool ParentTreeIdle(Transition transition)
        => !transition.IsPreparing && !transition.IsRunning && transition.Animations.All(x => x.IsFinished) &&
           transition.Transitions.All(IsFinished);

    internal bool KeysEqual(object? a, object? b)
        => EqualityComparer<object?>.Default.Equals(KeyOf(a), KeyOf(b));

    private object? KeyOf(object? state) => ContentKeySelector?.Invoke(state) ?? state;

    internal ContentSlot? SlotFor(object? state)
    {
        foreach (var slot in _currentlyVisible)
            if (KeysEqual(slot.State, state)) return slot;
        return null;
    }

    private int IndexOfKey(object? state)
    {
        for (var i = 0; i < _currentlyVisible.Count; i++)
            if (KeysEqual(_currentlyVisible[i].State, state)) return i;
        return -1;
    }

    private ContentSlot? TargetSlot() => _currentlyVisible.Count == 0 ? null : _currentlyVisible[^1];

    private void Disconnect()
    {
        _scopeBinding.Reset();
        var connection = _connection;
        _connection = null;
        if (connection is not null)
        {
            connection.Transition.PhaseChanged -= OnPhaseChanged;
            connection.Transition.TargetChanging -= OnParentTargetChanging;
        }
        try
        {
            foreach (var slot in _currentlyVisible.ToArray()) DisposeSlot(slot);
            _panel?.ResetSize();
            _fallbackTransform = null;
            _resolvedTransform = null;
            SetAndRaise(VisibleSlotCountProperty, ref _visibleSlotCount, 0);
            SetAndRaise(TargetScopeProperty, ref _targetScope, null);
            SetAndRaise(IsIdleProperty, ref _isIdle, true);
        }
        finally { connection?.Dispose(); }
    }

    private async Task RunAsync(ContentConnection connection, FrameScope scope, CancellationToken cancellationToken)
    {
        try { await connection.Loop.RunAsync(scope, cancellationToken); }
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
}

public sealed class ContentSlotEventArgs(AnimatedVisibilityScope scope, object? content) : EventArgs
{
    public AnimatedVisibilityScope Scope { get; } = scope;
    public object? Content { get; } = content;
}

internal sealed class AnimatedContentTransitionScope(
    AnimatedContent host, object? initial, object? target) : IAnimatedContentTransitionScope
{
    public object? InitialState { get; } = initial;
    public object? TargetState { get; } = target;
    public bool IsTransitioningTo(object? initialState, object? targetState)
        => Equals(InitialState, initialState) && Equals(TargetState, targetState);
    public VisibilityAlignment ContentAlignment => host.ContentAlignment;
    public Size CurrentSize => host.CurrentSize;
    public Size TargetContentSize => host.TargetContentSize;
    public FlowDirection FlowDirection => host.FlowDirection;

    public EnterTransition SlideIntoContainer(ContentSlideDirection towards,
        IFiniteAnimationSpec<Vector>? spec = null, Func<double, double>? offset = null)
    {
        offset ??= static x => x;
        spec ??= VisibilityEffects.Spring<Vector>();
        return towards switch
        {
            _ when IsLeft(towards) => new SlideInHorizontally
            {
                Spec = spec,
                InitialOffsetSelector = width => offset(host.CurrentSize.Width -
                    VisibilityMotion.Align(new Size(width, width), host.CurrentSize, host.ContentAlignment, FlowDirection.LeftToRight).X)
            },
            _ when IsRight(towards) => new SlideInHorizontally
            {
                Spec = spec,
                InitialOffsetSelector = width => offset(
                    -VisibilityMotion.Align(new Size(width, width), host.CurrentSize, host.ContentAlignment, FlowDirection.LeftToRight).X - width)
            },
            ContentSlideDirection.Up => new SlideInVertically
            {
                Spec = spec,
                InitialOffsetSelector = height => offset(host.CurrentSize.Height -
                    VisibilityMotion.Align(new Size(height, height), host.CurrentSize, host.ContentAlignment, FlowDirection.LeftToRight).Y)
            },
            ContentSlideDirection.Down => new SlideInVertically
            {
                Spec = spec,
                InitialOffsetSelector = height => offset(
                    -VisibilityMotion.Align(new Size(height, height), host.CurrentSize, host.ContentAlignment, FlowDirection.LeftToRight).Y - height)
            },
            _ => EnterTransition.None
        };
    }

    public ExitTransition SlideOutOfContainer(ContentSlideDirection towards,
        IFiniteAnimationSpec<Vector>? spec = null, Func<double, double>? offset = null)
    {
        offset ??= static x => x;
        spec ??= VisibilityEffects.Spring<Vector>();
        return towards switch
        {
            _ when IsLeft(towards) => new SlideOutHorizontally
            {
                Spec = spec,
                TargetOffsetSelector = width =>
                {
                    var target = host.TargetContentSize;
                    return offset(-VisibilityMotion.Align(new Size(width, width), target, host.ContentAlignment, FlowDirection.LeftToRight).X - width);
                }
            },
            _ when IsRight(towards) => new SlideOutHorizontally
            {
                Spec = spec,
                TargetOffsetSelector = width =>
                {
                    var target = host.TargetContentSize;
                    return offset(-VisibilityMotion.Align(new Size(width, width), target, host.ContentAlignment, FlowDirection.LeftToRight).X + target.Width);
                }
            },
            ContentSlideDirection.Up => new SlideOutVertically
            {
                Spec = spec,
                TargetOffsetSelector = height =>
                {
                    var target = host.TargetContentSize;
                    return offset(-VisibilityMotion.Align(new Size(height, height), target, host.ContentAlignment, FlowDirection.LeftToRight).Y - height);
                }
            },
            ContentSlideDirection.Down => new SlideOutVertically
            {
                Spec = spec,
                TargetOffsetSelector = height =>
                {
                    var target = host.TargetContentSize;
                    return offset(-VisibilityMotion.Align(new Size(height, height), target, host.ContentAlignment, FlowDirection.LeftToRight).Y + target.Height);
                }
            },
            _ => ExitTransition.None
        };
    }

    private bool IsLeft(ContentSlideDirection towards) => towards is ContentSlideDirection.Left
        || towards is ContentSlideDirection.Start && host.FlowDirection == FlowDirection.LeftToRight
        || towards is ContentSlideDirection.End && host.FlowDirection == FlowDirection.RightToLeft;

    private bool IsRight(ContentSlideDirection towards) => towards is ContentSlideDirection.Right
        || towards is ContentSlideDirection.Start && host.FlowDirection == FlowDirection.RightToLeft
        || towards is ContentSlideDirection.End && host.FlowDirection == FlowDirection.LeftToRight;
}
