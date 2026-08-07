// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/BottomSheetScaffold.kt:116-340,
// 453-620 and SheetDefaults.kt:90-260, 496-540 (VERSION 29.0.0).
// The alpha22 executable reference has the same standard two-anchor geometry. Avalonia maps
// suspend SheetState methods to synchronous, vetoable state commands and keeps animation owned by
// the control's MaterialMotionScope so interrupted drags resume from the current fraction.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using Material3Expressive.Controls.Automation;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialSkiaShadow;

namespace Material3Expressive.Controls;

/// <summary>Standard persistent bottom-sheet positions.</summary>
public enum BottomSheetValue
{
    Hidden,
    PartiallyExpanded,
    Expanded
}

/// <summary>
/// Vetoable state owner for <see cref="MaterialBottomSheetScaffold" />.
/// </summary>
public sealed class BottomSheetState : INotifyPropertyChanged
{
    private BottomSheetValue _currentValue;
    private BottomSheetValue _targetValue;

    public BottomSheetState(
        BottomSheetValue initialValue = BottomSheetValue.PartiallyExpanded,
        Func<BottomSheetValue, bool>? confirmValueChange = null)
    {
        if (!Enum.IsDefined(initialValue))
            throw new ArgumentOutOfRangeException(nameof(initialValue));

        _currentValue = _targetValue = initialValue;
        ConfirmValueChange = confirmValueChange;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public BottomSheetValue CurrentValue
    {
        get => _currentValue;
        private set
        {
            if (_currentValue == value)
                return;
            _currentValue = value;
            PropertyChanged?.Invoke(this, new(nameof(CurrentValue)));
        }
    }

    public BottomSheetValue TargetValue
    {
        get => _targetValue;
        private set
        {
            if (_targetValue == value)
                return;
            _targetValue = value;
            PropertyChanged?.Invoke(this, new(nameof(TargetValue)));
        }
    }

    /// <summary>Called before a command changes the target; return false to veto it.</summary>
    public Func<BottomSheetValue, bool>? ConfirmValueChange { get; set; }

    public bool Expand() => TrySet(BottomSheetValue.Expanded);

    public bool PartiallyExpand() => TrySet(BottomSheetValue.PartiallyExpanded);

    public bool Hide() => TrySet(BottomSheetValue.Hidden);

    internal bool TrySet(BottomSheetValue value)
    {
        if (!Enum.IsDefined(value) || (ConfirmValueChange?.Invoke(value) == false))
            return false;

        TargetValue = value;
        CurrentValue = value;
        return true;
    }
}

/// <summary>
/// Coordinates a standard persistent bottom sheet with body, top-bar, and snackbar slots.
/// </summary>
public sealed class MaterialBottomSheetScaffold : ContentControl
{
    public static readonly StyledProperty<object?> SheetContentProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffold, object?>(nameof(SheetContent));
    public static readonly StyledProperty<object?> TopBarProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffold, object?>(nameof(TopBar));
    public static readonly StyledProperty<object?> SnackbarHostProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffold, object?>(nameof(SnackbarHost));
    public static readonly StyledProperty<double> SheetPeekHeightProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffold, double>(
            nameof(SheetPeekHeight), 56, validate: static v => double.IsFinite(v) && v >= 0);
    public static readonly StyledProperty<double> SheetMaxWidthProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffold, double>(
            nameof(SheetMaxWidth), 640, validate: static v => double.IsFinite(v) && v > 0);
    public static readonly StyledProperty<bool> SheetSwipeEnabledProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffold, bool>(
            nameof(SheetSwipeEnabled), true);
    public static readonly DirectProperty<MaterialBottomSheetScaffold, BottomSheetState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialBottomSheetScaffold, BottomSheetState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));
    public static readonly DirectProperty<MaterialBottomSheetScaffold, Thickness> ContentPaddingProperty =
        AvaloniaProperty.RegisterDirect<MaterialBottomSheetScaffold, Thickness>(
            nameof(ContentPadding), owner => owner.ContentPadding);

    private BottomSheetState _state = new();
    private Thickness _contentPadding;
    private MaterialBottomSheetScaffoldSheet? _sheet;

    public MaterialBottomSheetScaffold()
    {
        _state.PropertyChanged += OnStateChanged;
    }

    public object? SheetContent
    {
        get => GetValue(SheetContentProperty);
        set => SetValue(SheetContentProperty, value);
    }

    public object? TopBar
    {
        get => GetValue(TopBarProperty);
        set => SetValue(TopBarProperty, value);
    }

    public object? SnackbarHost
    {
        get => GetValue(SnackbarHostProperty);
        set => SetValue(SnackbarHostProperty, value);
    }

    public double SheetPeekHeight
    {
        get => GetValue(SheetPeekHeightProperty);
        set => SetValue(SheetPeekHeightProperty, value);
    }

    public double SheetMaxWidth
    {
        get => GetValue(SheetMaxWidthProperty);
        set => SetValue(SheetMaxWidthProperty, value);
    }

    public bool SheetSwipeEnabled
    {
        get => GetValue(SheetSwipeEnabledProperty);
        set => SetValue(SheetSwipeEnabledProperty, value);
    }

    /// <summary>Gets or sets the persistent sheet state. The getter is always non-null.</summary>
    public BottomSheetState State
    {
        get => _state;
        set => SetState(value);
    }

    public Thickness ContentPadding => _contentPadding;

    internal BottomSheetState EffectiveState => _state;

    /// <summary>Replaces the current state with a fresh default partial state.</summary>
    public void ResetState() => SetState(null);

    internal void SetContentPadding(Thickness value)
    {
        if (_contentPadding == value)
            return;
        SetAndRaise(ContentPaddingProperty, ref _contentPadding, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SheetPeekHeightProperty
                 || change.Property == SheetMaxWidthProperty
                 || change.Property == SheetContentProperty
                 || change.Property == TopBarProperty
                 || change.Property == SnackbarHostProperty)
        {
            InvalidateMeasure();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _sheet = e.NameScope.Find<MaterialBottomSheetScaffoldSheet>("PART_Sheet")
            ?? throw new InvalidOperationException(
                "MaterialBottomSheetScaffold template is missing 'PART_Sheet'.");
        _sheet.State = _state;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void SetState(BottomSheetState? value)
    {
        var next = value ?? new BottomSheetState();
        if (ReferenceEquals(_state, next))
            return;

        _state.PropertyChanged -= OnStateChanged;
        SetAndRaise(StateProperty, ref _state, next);
        _state.PropertyChanged += OnStateChanged;
        if (_sheet is not null)
            _sheet.State = _state;
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialBottomSheetScaffoldAutomationPeer(this);
}

/// <summary>Internal themed surface used by the scaffold template.</summary>
internal sealed class MaterialBottomSheetScaffoldSheet : ContentControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    public static readonly StyledProperty<double> PeekHeightProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffoldSheet, double>(nameof(PeekHeight), 56);
    public static readonly StyledProperty<bool> SwipeEnabledProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffoldSheet, bool>(nameof(SwipeEnabled), true);
    public static readonly DirectProperty<MaterialBottomSheetScaffoldSheet, BottomSheetState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialBottomSheetScaffoldSheet, BottomSheetState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));

    private readonly MaterialElementContext _theme;
    private readonly MaterialMotionScope _motion;
    private BottomSheetTokens _tokens = BottomSheetTokens.Standard;
    private BottomSheetColors _colors = BottomSheetTokens.Standard.Resolve(null, false);
    private Point? _dragStart;
    private double _dragStartFraction;
    private bool _dragging;
    private ScrollViewer? _dragScrollViewer;
    private int? _nestedGestureId;
    private double _fraction = 1;
    private BottomSheetState _state = new();
    private BottomSheetState? _observedState;

    public MaterialBottomSheetScaffoldSheet()
    {
        _theme = MaterialElementContext.GetOrCreate(this);
        _motion = MaterialElementContext.GetOrCreate(this).Motion;
        AddHandler(
            InputElement.ScrollGestureEvent,
            OnNestedScrollGesture,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        AddHandler(
            InputElement.ScrollGestureEndedEvent,
            OnNestedScrollGestureEnded,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        AddHandler(
            InputElement.PointerWheelChangedEvent,
            OnNestedPointerWheelChanged,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        ApplyTheme();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTheme();

    void IMaterialMotionConsumer.OnMaterialMotionFrame()
    {
        _fraction = _motionFraction?.Value ?? _fraction;
        (this.GetVisualParent() as Control)?.InvalidateArrange();
        InvalidateVisual();
    }

    static MaterialBottomSheetScaffoldSheet() =>
        ClipToBoundsProperty.OverrideDefaultValue<MaterialBottomSheetScaffoldSheet>(false);

    public double PeekHeight
    {
        get => GetValue(PeekHeightProperty);
        set => SetValue(PeekHeightProperty, value);
    }

    public bool SwipeEnabled
    {
        get => GetValue(SwipeEnabledProperty);
        set => SetValue(SwipeEnabledProperty, value);
    }

    public BottomSheetState State
    {
        get => _state;
        set => SetState(value);
    }

    private double Fraction =>
        _fraction;

    internal double SheetFraction => _fraction;

    internal double SheetTargetFraction => _motionFraction?.Target ?? _fraction;

    // The parity adapter consumes the same shape and shadow inputs used by Render. This is
    // structural evidence for the standard sheet's asymmetric CornerExtraLargeTop elevation.
    internal ComposeShadowShape ParityShadowShape =>
        ComposeShadowShape.RoundedRectangle(new Rect(Bounds.Size), ContainerCornerRadius);

    internal ComposeShadowParameters ParityShadowParameters =>
        ComposeShadowParameters.Material(_tokens.ContainerElevation, ShadowColor);

    internal bool ParityShadowOccluderDrawn => true;

    private SpringAnimation? _motionFraction;

    private CornerRadius ContainerCornerRadius =>
        _theme.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, Bounds.Size);

    private Color ShadowColor =>
        _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark) ?? Colors.Black;

    public override void Render(DrawingContext context)
    {
        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            ContainerCornerRadius,
            _colors.ContainerColor,
            outline: null,
            outlineWidth: 0,
            elevation: _tokens.ContainerElevation,
            shadowColor: ShadowColor);
        base.Render(context);
        var handle = new Rect(
            (Bounds.Width - _tokens.DragHandleWidth) / 2,
            _tokens.DragHandleSpace,
            _tokens.DragHandleWidth,
            _tokens.DragHandleHeight);
        context.DrawRectangle(
            _colors.DragHandleColor,
            null,
            new RoundedRect(handle, new CornerRadius(_tokens.DragHandleHeight / 2)));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!SwipeEnabled)
            return;

        _dragStart = e.GetPosition(this);
        _dragStartFraction = Fraction;
        _dragScrollViewer = FindScrollViewer(e.Source);
        _dragging = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragStart is not { } start || Bounds.Height <= 0)
            return;

        var delta = e.GetPosition(this).Y - start.Y;
        if (!_dragging)
        {
            if (Math.Abs(delta) < 4)
                return;

            var canMoveUp = delta < 0 && _dragStartFraction > 0;
            var canMoveDown =
                delta > 0
                && _dragStartFraction < 1
                && (_dragScrollViewer is null || _dragScrollViewer.Offset.Y <= 0.5);
            if (!canMoveUp && !canMoveDown)
                return;

            _dragging = true;
            _motionFraction?.SnapTo(_dragStartFraction);
            e.Pointer.Capture(this);
        }

        SetFraction(_dragStartFraction + delta / TravelDistance);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var wasDragging = _dragging;
        _dragging = false;
        _dragStart = null;
        _dragScrollViewer = null;
        if (!wasDragging)
            return;

        e.Pointer.Capture(null);
        CommitGestureTarget();
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        var wasDragging = _dragging;
        _dragging = false;
        _dragStart = null;
        _dragScrollViewer = null;
        if (wasDragging)
            CommitGestureTarget();
    }

    internal double ConsumeNestedScrollDelta(double deltaY, bool childConsumed)
    {
        if (!SwipeEnabled || Math.Abs(deltaY) < double.Epsilon)
            return 0;

        var consumed = 0d;
        if (deltaY > 0 && Fraction > 0)
        {
            consumed = Math.Min(deltaY, Fraction * TravelDistance);
        }
        else if (deltaY < 0 && Fraction < 1 && !childConsumed)
        {
            consumed = -Math.Min(-deltaY, (1 - Fraction) * TravelDistance);
        }

        if (consumed == 0)
            return 0;

        _motionFraction?.SnapTo(Fraction);
        SetFraction(Fraction - consumed / TravelDistance);
        return consumed;
    }

    internal void EndNestedScrollGesture() =>
        CommitGestureTarget();

    private double TravelDistance =>
        Math.Max(1, Bounds.Height - Math.Clamp(PeekHeight, 0, Bounds.Height));

    private void SetFraction(double fraction)
    {
        fraction = Math.Clamp(fraction, 0, 1);
        _motionFraction ??= _motion.AddSpring(fraction, _theme.Spring(MotionRole.DefaultSpatial));
        _motionFraction.SnapTo(fraction);
        _fraction = fraction;
        (this.GetVisualParent() as Control)?.InvalidateArrange();
        InvalidateVisual();
    }

    private void CommitGestureTarget()
    {
        if (State is not { } state)
            return;

        var target = Fraction < 0.5
            ? BottomSheetValue.Expanded
            : BottomSheetValue.PartiallyExpanded;
        if (!state.TrySet(target))
            target = state.CurrentValue;

        // TrySet does not raise when an interrupted animation is recommitted to its existing
        // logical target. Explicitly resume the spring from the gesture's current fraction.
        AnimateTo(ValueFraction(target), snap: false);
    }

    private void OnNestedScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        var viewer = FindScrollViewer(e.Source);
        var childConsumed = e.Handled && viewer is not null;
        var consumed = ConsumeNestedScrollDelta(e.Delta.Y, childConsumed);
        if (consumed == 0)
            return;

        // Compose pre-consumes upward motion while the sheet can expand. Avalonia's scroll event
        // bubbles after ScrollViewer, so undo only the portion transferred to the sheet.
        if (consumed > 0 && childConsumed && viewer is not null)
        {
            viewer.SetCurrentValue(
                ScrollViewer.OffsetProperty,
                viewer.Offset.WithY(Math.Max(0, viewer.Offset.Y - consumed)));
        }

        _nestedGestureId = e.Id;
        e.Handled = true;
    }

    private void OnNestedScrollGestureEnded(object? sender, ScrollGestureEndedEventArgs e)
    {
        if (_nestedGestureId != e.Id)
            return;

        _nestedGestureId = null;
        EndNestedScrollGesture();
        e.Handled = true;
    }

    private void OnNestedPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!SwipeEnabled || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            return;

        var viewer = FindScrollViewer(e.Source);
        BottomSheetValue? target = null;
        if (e.Delta.Y > 0 && Fraction > 0)
            target = BottomSheetValue.Expanded;
        else if (e.Delta.Y < 0
                 && Fraction < 1
                 && (viewer is null || viewer.Offset.Y <= 0.5))
            target = BottomSheetValue.PartiallyExpanded;

        if (target is not { } value || !State.TrySet(value))
            return;

        AnimateTo(ValueFraction(value), snap: false);
        e.Handled = true;
    }

    private ScrollViewer? FindScrollViewer(object? source)
    {
        if (source is ScrollViewer viewer)
            return viewer;
        if (source is not Visual visual)
            return null;

        return visual
            .GetVisualAncestors()
            .TakeWhile(ancestor => !ReferenceEquals(ancestor, this))
            .OfType<ScrollViewer>()
            .FirstOrDefault();
    }

    private void ApplyTheme()
    {
        _tokens = BottomSheetTokens.Standard;
        _colors = _tokens.Resolve(_theme.Theme.ColorScheme, _theme.IsDark);
        // DragHandle uses 22dp vertical padding on both sides around its 4dp stroke.
        Padding = new Thickness(
            0,
            _tokens.DragHandleSpace * 2 + _tokens.DragHandleHeight,
            0,
            0);
        ObserveState(State);
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty)
            ObserveState(change.GetNewValue<BottomSheetState>());
    }

    private void ObserveState(BottomSheetState state)
    {
        if (ReferenceEquals(_observedState, state))
            return;
        if (_observedState is not null)
            _observedState.PropertyChanged -= OnStateChanged;
        _observedState = state;
        if (_observedState is not null)
        {
            _observedState.PropertyChanged += OnStateChanged;
            AnimateTo(ValueFraction(_observedState.CurrentValue), snap: true);
        }
    }

    private void SetState(BottomSheetState? value)
    {
        var next = value ?? new BottomSheetState();
        if (ReferenceEquals(_state, next))
            return;
        SetAndRaise(StateProperty, ref _state, next);
        ObserveState(next);
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is BottomSheetState state && e.PropertyName is nameof(BottomSheetState.CurrentValue))
            AnimateTo(ValueFraction(state.CurrentValue), snap: false);
    }

    private void AnimateTo(double target, bool snap)
    {
        _motionFraction ??= _motion.AddSpring(target, _theme.Spring(MotionRole.DefaultSpatial));
        if (snap)
            _motionFraction.SnapTo(target);
        else
        {
            _motionFraction.AnimateTo(target, _theme.Spring(MotionRole.DefaultSpatial));
            _motion.Start();
        }
        _fraction = _motionFraction.Value;
        (this.GetVisualParent() as Control)?.InvalidateArrange();
        InvalidateVisual();
    }

    private static double ValueFraction(BottomSheetValue value) =>
        value switch
        {
            BottomSheetValue.Expanded => 0,
            BottomSheetValue.Hidden => 2,
            _ => 1
        };
}
