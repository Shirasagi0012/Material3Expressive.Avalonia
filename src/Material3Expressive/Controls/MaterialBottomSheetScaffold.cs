// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/BottomSheetScaffold.kt:116-340,
// 453-620 and SheetDefaults.kt:90-260, 496-540 (VERSION 29.0.0).
// The alpha22 executable reference has the same standard two-anchor geometry. Avalonia maps
// suspend SheetState methods to synchronous, vetoable state commands and keeps animation owned by
// the control's MotionGroup so interrupted drags resume from the current fraction.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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
    public static readonly StyledProperty<BottomSheetState?> StateProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffold, BottomSheetState?>(
            nameof(State), defaultValue: null);
    public static readonly DirectProperty<MaterialBottomSheetScaffold, Thickness> ContentPaddingProperty =
        AvaloniaProperty.RegisterDirect<MaterialBottomSheetScaffold, Thickness>(
            nameof(ContentPadding), owner => owner.ContentPadding);

    private BottomSheetState _state = new();
    private Thickness _contentPadding;

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

    /// <summary>Gets or sets the persistent sheet state. Null restores a fresh partial state.</summary>
    public BottomSheetState? State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public Thickness ContentPadding => _contentPadding;

    internal BottomSheetState EffectiveState => _state;

    internal void SetContentPadding(Thickness value)
    {
        if (_contentPadding == value)
            return;
        SetAndRaise(ContentPaddingProperty, ref _contentPadding, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty)
        {
            _state.PropertyChanged -= OnStateChanged;
            _state = State ?? new BottomSheetState();
            _state.PropertyChanged += OnStateChanged;
            InvalidateMeasure();
            InvalidateVisual();
        }
        else if (change.Property == SheetPeekHeightProperty
                 || change.Property == SheetMaxWidthProperty
                 || change.Property == SheetContentProperty
                 || change.Property == TopBarProperty
                 || change.Property == SnackbarHostProperty)
        {
            InvalidateMeasure();
        }
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialBottomSheetScaffoldAutomationPeer(this);
}

/// <summary>Internal themed surface used by the scaffold template.</summary>
internal sealed class MaterialBottomSheetScaffoldSheet : ContentControl
{
    public static readonly StyledProperty<double> PeekHeightProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffoldSheet, double>(nameof(PeekHeight), 56);
    public static readonly StyledProperty<bool> SwipeEnabledProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffoldSheet, bool>(nameof(SwipeEnabled), true);
    public static readonly StyledProperty<BottomSheetState?> StateProperty =
        AvaloniaProperty.Register<MaterialBottomSheetScaffoldSheet, BottomSheetState?>(nameof(State));

    private readonly MaterialThemeWatcher _theme;
    private readonly MotionGroup _motion;
    private BottomSheetTokens _tokens = BottomSheetTokens.Standard;
    private BottomSheetColors _colors = BottomSheetTokens.Standard.Resolve(null, false);
    private Point? _dragStart;
    private double _dragStartFraction;
    private double _fraction = 1;
    private BottomSheetState? _observedState;

    public MaterialBottomSheetScaffoldSheet()
    {
        _theme = new MaterialThemeWatcher(this);
        _motion = new MotionGroup(this);
        _motion.Ticked += (_, _) =>
        {
            _fraction = _motionFraction?.Value ?? _fraction;
            (this.GetVisualParent() as Control)?.InvalidateArrange();
            InvalidateVisual();
        };
        _theme.Changed += (_, _) => ApplyTheme();
        ApplyTheme();
    }

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

    public BottomSheetState? State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    private double Fraction =>
        _fraction;

    internal double SheetFraction => _fraction;

    private SpringAnimation? _motionFraction;

    public override void Render(DrawingContext context)
    {
        var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, Bounds.Size);
        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            corners,
            _colors.ContainerColor,
            outline: null,
            outlineWidth: 0,
            elevation: _tokens.ContainerElevation,
            shadowColor: _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark)
                         ?? Colors.Black);
        base.Render(context);
        var handle = new Rect(
            (Bounds.Width - _tokens.DragHandleWidth) / 2,
            (_tokens.DragHandleSpace - _tokens.DragHandleHeight) / 2,
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
        if (!SwipeEnabled || State is null)
            return;

        _dragStart = e.GetPosition(this);
        _dragStartFraction = Fraction;
        _motionFraction?.SnapTo(_dragStartFraction);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragStart is not { } start || Bounds.Height <= 0)
            return;
        var delta = e.GetPosition(this).Y - start.Y;
        var fraction = Math.Clamp(_dragStartFraction + delta / Math.Max(1, Bounds.Height), 0, 1);
        _motionFraction?.SnapTo(fraction);
        _fraction = fraction;
        (this.GetVisualParent() as Control)?.InvalidateArrange();
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragStart = null;
        e.Pointer.Capture(null);
        if (Fraction < 0.5)
            State?.Expand();
        else
            State?.PartiallyExpand();
    }

    private void ApplyTheme()
    {
        _tokens = BottomSheetTokens.Standard;
        _colors = _tokens.Resolve(_theme.Theme.ColorScheme, _theme.IsDark);
        Padding = new Thickness(0, _tokens.DragHandleSpace, 0, 0);
        ObserveState(State);
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty)
            ObserveState(change.GetNewValue<BottomSheetState?>());
    }

    private void ObserveState(BottomSheetState? state)
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
