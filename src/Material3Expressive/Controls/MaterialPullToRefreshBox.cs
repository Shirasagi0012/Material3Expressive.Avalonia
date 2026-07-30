// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/pulltorefresh/
// PullToRefresh.kt. The alpha22 executable reference exposes the same pull multiplier, threshold,
// nonlinear tension, nested-scroll ordering, and default indicator geometry.
//
// F5 / Ctrl+R, Escape, Invoke, and read-only RangeValue are the explicit Avalonia desktop and
// accessibility adaptations tracked by open decision D003.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialSkiaShadow;

namespace Material3Expressive.Controls;

/// <summary>Default dimensions and pull physics for <see cref="MaterialPullToRefreshBox" />.</summary>
public static class PullToRefreshDefaults
{
    /// <summary>The adjusted pull distance that arms refresh.</summary>
    public const double PositionalThreshold = 80;

    /// <summary>The fraction of raw pointer travel applied to the indicator.</summary>
    public const double DragMultiplier = .5;

    /// <summary>The default circular indicator container diameter.</summary>
    public const double IndicatorContainerSize = 40;

    /// <summary>The default indicator's maximum travel distance.</summary>
    public const double IndicatorMaxDistance = 80;

    internal const double PointerSlop = 4;
}

/// <summary>
/// Recoverable pull progress for <see cref="MaterialPullToRefreshBox" />.
/// </summary>
/// <remarks>
/// Persist <see cref="DistanceFraction" /> and pass it to the constructor to restore custom
/// indicator progress. A mounted box subsequently treats <c>IsRefreshing</c> as application-owned:
/// changing it retargets this state to the threshold or hidden anchor.
/// </remarks>
public sealed class PullToRefreshState : INotifyPropertyChanged
{
    private double _distanceFraction;
    private bool _isAnimating;

    /// <summary>Creates state at a non-negative distance fraction.</summary>
    public PullToRefreshState(double initialDistanceFraction = 0)
    {
        ValidateFraction(initialDistanceFraction, nameof(initialDistanceFraction));
        _distanceFraction = initialDistanceFraction;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    internal event EventHandler<PullTransitionRequestedEventArgs>? TransitionRequested;

    /// <summary>
    /// Current vertical distance divided by the threshold. Overshoot can raise this as high as 2.
    /// </summary>
    public double DistanceFraction => _distanceFraction;

    /// <summary>Whether the control-owned spring is currently moving this state.</summary>
    public bool IsAnimating => _isAnimating;

    /// <summary>Animates the indicator to one threshold of visible distance.</summary>
    public void AnimateToThreshold() => RequestTransition(1, snap: false);

    /// <summary>Animates the indicator above the viewport.</summary>
    public void AnimateToHidden() => RequestTransition(0, snap: false);

    /// <summary>Immediately moves to an arbitrary non-negative fraction.</summary>
    public void SnapTo(double targetValue)
    {
        ValidateFraction(targetValue, nameof(targetValue));
        RequestTransition(targetValue, snap: true);
    }

    internal void SetMotionValue(double value, bool isAnimating)
    {
        value = Math.Max(0, value);
        if (Math.Abs(_distanceFraction - value) >= .000001)
        {
            _distanceFraction = value;
            PropertyChanged?.Invoke(this, new(nameof(DistanceFraction)));
        }

        if (_isAnimating == isAnimating)
            return;
        _isAnimating = isAnimating;
        PropertyChanged?.Invoke(this, new(nameof(IsAnimating)));
    }

    private void RequestTransition(double targetValue, bool snap)
    {
        var args = new PullTransitionRequestedEventArgs(targetValue, snap);
        TransitionRequested?.Invoke(this, args);
        if (!args.Handled)
            SetMotionValue(targetValue, isAnimating: false);
    }

    private static void ValidateFraction(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Pull-to-refresh distance must be finite and non-negative.");
        }
    }
}

internal sealed class PullTransitionRequestedEventArgs(double targetValue, bool snap) : EventArgs
{
    public double TargetValue { get; } = targetValue;

    public bool Snap { get; } = snap;

    public bool Handled { get; set; }
}

/// <summary>
/// Hosts scrollable content and requests refresh after a downward pull crosses its threshold.
/// </summary>
public sealed class MaterialPullToRefreshBox : ContentControl
{
    /// <summary>Defines whether the application is currently refreshing.</summary>
    public static readonly StyledProperty<bool> IsRefreshingProperty =
        AvaloniaProperty.Register<MaterialPullToRefreshBox, bool>(nameof(IsRefreshing));

    /// <summary>Defines whether pointer and nested-scroll pull gestures are enabled.</summary>
    public static readonly StyledProperty<bool> PullEnabledProperty =
        AvaloniaProperty.Register<MaterialPullToRefreshBox, bool>(nameof(PullEnabled), true);

    /// <summary>Defines the adjusted distance that arms refresh.</summary>
    public static readonly StyledProperty<double> ThresholdProperty =
        AvaloniaProperty.Register<MaterialPullToRefreshBox, double>(
            nameof(Threshold),
            PullToRefreshDefaults.PositionalThreshold,
            validate: static value => double.IsFinite(value) && value > 0);

    /// <summary>Defines optional content that replaces the default circular indicator.</summary>
    public static readonly StyledProperty<object?> IndicatorContentProperty =
        AvaloniaProperty.Register<MaterialPullToRefreshBox, object?>(nameof(IndicatorContent));

    /// <summary>Defines the replaceable, non-null progress state.</summary>
    public static readonly DirectProperty<MaterialPullToRefreshBox, PullToRefreshState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialPullToRefreshBox, PullToRefreshState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));

    /// <summary>Defines the indicator object selected by the template.</summary>
    public static readonly DirectProperty<MaterialPullToRefreshBox, object> EffectiveIndicatorProperty =
        AvaloniaProperty.RegisterDirect<MaterialPullToRefreshBox, object>(
            nameof(EffectiveIndicator), owner => owner.EffectiveIndicator);

    /// <summary>Defines the default indicator's current Y translation.</summary>
    public static readonly DirectProperty<MaterialPullToRefreshBox, double> IndicatorOffsetProperty =
        AvaloniaProperty.RegisterDirect<MaterialPullToRefreshBox, double>(
            nameof(IndicatorOffset), owner => owner._indicatorOffset);

    private static readonly SpringSpec DefaultAnimatableSpring = new(1, 1500, .01);

    private readonly MotionGroup _motion;
    private readonly MaterialPullToRefreshIndicator _defaultIndicator;
    private PullToRefreshState _state = new();
    private SpringAnimation? _fractionMotion;
    private object _effectiveIndicator;
    private double _indicatorOffset = -PullToRefreshDefaults.IndicatorContainerSize;
    private double _rawDistance;
    private bool _pulling;
    private bool _pointerArmed;
    private Point _lastPointerPosition;
    private ScrollViewer? _pointerScrollViewer;
    private IPointer? _capturedPointer;
    private int? _nestedGestureId;
    private bool _refreshRequestedThisGesture;
    private double _lastReportedFraction;

    /// <summary>Creates a pull-to-refresh host with independent progress state.</summary>
    public MaterialPullToRefreshBox()
    {
        Focusable = true;
        _defaultIndicator = new MaterialPullToRefreshIndicator { Name = "indicator" };
        _effectiveIndicator = _defaultIndicator;
        _motion = new MotionGroup(this);
        _motion.Ticked += OnMotionTick;
        AttachState(_state);
        UpdateIndicator();

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
    }

    static MaterialPullToRefreshBox() =>
        ClipToBoundsProperty.OverrideDefaultValue<MaterialPullToRefreshBox>(false);

    /// <summary>
    /// Raised when a gesture, keyboard command, or automation action requests application refresh.
    /// The application owns <see cref="IsRefreshing" /> and completion.
    /// </summary>
    public event EventHandler? RefreshRequested;

    internal event EventHandler? ProgressChanged;

    /// <summary>Whether the application is currently refreshing.</summary>
    public bool IsRefreshing
    {
        get => GetValue(IsRefreshingProperty);
        set => SetValue(IsRefreshingProperty, value);
    }

    /// <summary>Whether pointer and nested-scroll pull gestures are enabled.</summary>
    public bool PullEnabled
    {
        get => GetValue(PullEnabledProperty);
        set => SetValue(PullEnabledProperty, value);
    }

    /// <summary>The adjusted pull distance that arms refresh.</summary>
    public double Threshold
    {
        get => GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    /// <summary>Optional content that replaces the default indicator.</summary>
    public object? IndicatorContent
    {
        get => GetValue(IndicatorContentProperty);
        set => SetValue(IndicatorContentProperty, value);
    }

    /// <summary>Gets or replaces progress state. The getter is always non-null.</summary>
    public PullToRefreshState State
    {
        get => _state;
        set => SetState(value);
    }

    /// <summary>The indicator object consumed by the default template.</summary>
    public object EffectiveIndicator => _effectiveIndicator;

    /// <summary>The indicator's current Y translation.</summary>
    public double IndicatorOffset => _indicatorOffset;

    /// <summary>Replaces current state with fresh hidden state.</summary>
    public void ResetState() => SetState(null);

    internal double RawDistance => _rawDistance;

    internal double MotionTargetFraction => _fractionMotion?.Target ?? State.DistanceFraction;

    internal bool BeginPull(bool childAtStart = true)
    {
        if (!PullEnabled || IsRefreshing || (!childAtStart && State.DistanceFraction <= 0))
            return false;

        EnsureMotion();
        _fractionMotion!.SnapTo(State.DistanceFraction);
        _rawDistance = RawDistanceForFraction(State.DistanceFraction, Threshold);
        _pulling = true;
        _refreshRequestedThisGesture = false;
        State.SetMotionValue(State.DistanceFraction, isAnimating: false);
        return true;
    }

    /// <summary>
    /// Applies raw downward travel. Positive values pull; negative values retract.
    /// Returns the raw travel consumed by this control.
    /// </summary>
    internal double PullBy(double rawDelta, bool childAtStart = true)
    {
        if (!double.IsFinite(rawDelta) || Math.Abs(rawDelta) < double.Epsilon)
            return 0;
        if (!_pulling && !BeginPull(childAtStart))
            return 0;
        if (IsRefreshing || (rawDelta > 0 && !childAtStart && _rawDistance <= 0))
            return 0;

        var previous = _rawDistance;
        _rawDistance = Math.Max(0, previous + rawDelta);
        var consumed = _rawDistance - previous;
        if (Math.Abs(consumed) < double.Epsilon && previous <= 0)
            return 0;

        SetDraggedFraction(FractionForRawDistance(_rawDistance, Threshold));
        return rawDelta > 0 ? rawDelta : consumed;
    }

    /// <summary>
    /// Ends a pull and returns the downward fling velocity consumed by the visible indicator.
    /// Upward velocity remains available to the child, matching nested-scroll pre-fling ordering.
    /// </summary>
    internal double EndPull(double velocity = 0)
    {
        if (!_pulling)
            return 0;

        _pulling = false;
        var consumeVelocity = !IsRefreshing
                              && _rawDistance > 0
                              && velocity >= 0
            ? velocity
            : 0;
        if (!IsRefreshing
            && State.DistanceFraction > 1
            && !_refreshRequestedThisGesture)
        {
            _refreshRequestedThisGesture = true;
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        if (IsRefreshing)
        {
            _rawDistance = Threshold / PullToRefreshDefaults.DragMultiplier;
            AnimateFraction(1, snap: false);
        }
        else
        {
            _rawDistance = 0;
            AnimateFraction(0, snap: false);
        }
        return consumeVelocity;
    }

    internal double ConsumeNestedScrollDelta(double rawDelta, bool childAtStart)
    {
        if (rawDelta < 0 && State.DistanceFraction > 0)
            return PullBy(rawDelta, childAtStart: true);
        if (rawDelta > 0 && childAtStart)
            return PullBy(rawDelta, childAtStart: true);
        return 0;
    }

    internal void EndNestedScrollGesture() => EndPull();

    internal bool RequestRefreshFromAutomation()
    {
        if (!IsEffectivelyEnabled || IsRefreshing)
            return false;
        RefreshRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }

    internal bool HasRefreshAction => IsEffectivelyEnabled && !IsRefreshing;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !PullEnabled || IsRefreshing)
            return;

        _pointerArmed = true;
        _lastPointerPosition = e.GetPosition(this);
        _pointerScrollViewer = FindScrollViewer(e.Source);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_pointerArmed)
            return;

        var point = e.GetPosition(this);
        var delta = point - _lastPointerPosition;
        if (!_pulling)
        {
            if (Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) < PullToRefreshDefaults.PointerSlop)
                return;
            if (Math.Abs(delta.X) > Math.Abs(delta.Y))
            {
                CancelPointerInteraction();
                return;
            }

            var atStart = _pointerScrollViewer is null || _pointerScrollViewer.Offset.Y <= .5;
            if ((delta.Y <= 0 || !atStart) && State.DistanceFraction <= 0)
            {
                CancelPointerInteraction();
                return;
            }
            if (!BeginPull(atStart))
            {
                CancelPointerInteraction();
                return;
            }

            e.Pointer.Capture(this);
            _capturedPointer = e.Pointer;
        }

        var consumed = PullBy(
            delta.Y,
            _pointerScrollViewer is null || _pointerScrollViewer.Offset.Y <= .5);
        _lastPointerPosition = point;
        if (Math.Abs(consumed) > double.Epsilon)
            e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var wasPulling = _pulling;
        _pointerArmed = false;
        _pointerScrollViewer = null;
        if (!wasPulling)
            return;

        EndPull();
        e.Pointer.Capture(null);
        _capturedPointer = null;
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pointerArmed = false;
        _pointerScrollViewer = null;
        _capturedPointer = null;
        if (_pulling)
            EndPull();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !IsRefreshing && State.DistanceFraction > 0)
        {
            _rawDistance = 0;
            _pulling = false;
            State.AnimateToHidden();
            e.Handled = true;
            return;
        }

        if ((e.Key == Key.F5
             || (e.Key == Key.R && e.KeyModifiers.HasFlag(KeyModifiers.Control)))
            && RequestRefreshFromAutomation())
        {
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsRefreshingProperty)
        {
            CancelPointerInteraction();
            _rawDistance = IsRefreshing
                ? Threshold / PullToRefreshDefaults.DragMultiplier
                : 0;
            AnimateFraction(IsRefreshing ? 1 : 0, snap: TopLevel.GetTopLevel(this) is null);
            UpdateIndicator();
        }
        else if (change.Property == IndicatorContentProperty)
        {
            var next = IndicatorContent ?? _defaultIndicator;
            SetAndRaise(EffectiveIndicatorProperty, ref _effectiveIndicator, next);
        }
        else if (change.Property == ThresholdProperty)
        {
            _rawDistance = RawDistanceForFraction(State.DistanceFraction, Threshold);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialPullToRefreshBoxAutomationPeer(this);

    private void OnNestedScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        if (!PullEnabled || IsRefreshing)
            return;

        var viewer = FindScrollViewer(e.Source);
        var atStart = viewer is null || viewer.Offset.Y <= .5;
        var rawDelta = -e.Delta.Y;
        var consumed = ConsumeNestedScrollDelta(rawDelta, atStart);
        if (Math.Abs(consumed) < double.Epsilon)
            return;

        // Avalonia bubbles after ScrollViewer. Upward motion must retract the pull first, so undo
        // only the part the child received before this post-order handler observed it.
        if (consumed < 0 && e.Handled && viewer is not null)
        {
            viewer.SetCurrentValue(
                ScrollViewer.OffsetProperty,
                viewer.Offset.WithY(Math.Max(0, viewer.Offset.Y + consumed)));
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

    private void SetState(PullToRefreshState? value)
    {
        var next = value ?? new PullToRefreshState();
        if (ReferenceEquals(_state, next))
            return;

        CancelPointerInteraction();
        DetachState(_state);
        SetAndRaise(StateProperty, ref _state, next);
        AttachState(next);
        EnsureMotion();
        _fractionMotion!.SnapTo(next.DistanceFraction);
        _rawDistance = RawDistanceForFraction(next.DistanceFraction, Threshold);
        next.SetMotionValue(next.DistanceFraction, isAnimating: false);
        UpdateIndicator();
    }

    private void AttachState(PullToRefreshState state)
    {
        state.TransitionRequested += OnTransitionRequested;
        state.PropertyChanged += OnStatePropertyChanged;
    }

    private void DetachState(PullToRefreshState state)
    {
        state.TransitionRequested -= OnTransitionRequested;
        state.PropertyChanged -= OnStatePropertyChanged;
    }

    private void OnTransitionRequested(object? sender, PullTransitionRequestedEventArgs e)
    {
        if (!ReferenceEquals(sender, State))
            return;
        e.Handled = true;
        CancelPointerInteraction();
        _rawDistance = RawDistanceForFraction(e.TargetValue, Threshold);
        AnimateFraction(
            e.TargetValue,
            e.Snap || TopLevel.GetTopLevel(this) is null);
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PullToRefreshState.DistanceFraction))
            UpdateIndicator();
    }

    private void SetDraggedFraction(double fraction)
    {
        EnsureMotion();
        _fractionMotion!.SnapTo(fraction);
        State.SetMotionValue(fraction, isAnimating: false);
        UpdateIndicator();
    }

    private void AnimateFraction(double target, bool snap)
    {
        EnsureMotion();
        if (snap)
        {
            _fractionMotion!.SnapTo(target);
            State.SetMotionValue(target, isAnimating: false);
        }
        else
        {
            _fractionMotion!.AnimateTo(target, DefaultAnimatableSpring);
            State.SetMotionValue(_fractionMotion.Value, _fractionMotion.IsRunning);
            _motion.Start();
        }
        UpdateIndicator();
    }

    private void OnMotionTick(object? sender, EventArgs e)
    {
        if (_fractionMotion is null)
            return;
        State.SetMotionValue(_fractionMotion.Value, _fractionMotion.IsRunning);
        UpdateIndicator();
    }

    private void EnsureMotion() =>
        _fractionMotion ??= _motion.AddSpring(State.DistanceFraction, DefaultAnimatableSpring);

    private void UpdateIndicator()
    {
        if (Math.Abs(_lastReportedFraction - State.DistanceFraction) >= .000001)
        {
            _lastReportedFraction = State.DistanceFraction;
            ProgressChanged?.Invoke(this, EventArgs.Empty);
        }
        var nextOffset =
            State.DistanceFraction * PullToRefreshDefaults.IndicatorMaxDistance
            - PullToRefreshDefaults.IndicatorContainerSize;
        if (Math.Abs(_indicatorOffset - nextOffset) >= .000001)
            SetAndRaise(IndicatorOffsetProperty, ref _indicatorOffset, nextOffset);
        _defaultIndicator.Update(State.DistanceFraction, IsRefreshing);
    }

    private void CancelPointerInteraction()
    {
        _pointerArmed = false;
        _pulling = false;
        _pointerScrollViewer = null;
        _capturedPointer?.Capture(null);
        _capturedPointer = null;
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

    internal static double FractionForRawDistance(double rawDistance, double threshold)
    {
        var adjusted = Math.Max(0, rawDistance) * PullToRefreshDefaults.DragMultiplier;
        var progress = adjusted / threshold;
        if (progress <= 1)
            return progress;

        var linearTension = Math.Clamp(Math.Abs(progress) - 1, 0, 2);
        var tension = linearTension - linearTension * linearTension / 4;
        return 1 + tension;
    }

    internal static double RawDistanceForFraction(double fraction, double threshold)
    {
        if (fraction <= 1)
            return fraction * threshold / PullToRefreshDefaults.DragMultiplier;

        var tension = Math.Clamp(fraction - 1, 0, 1);
        var linearTension = 2 - 2 * Math.Sqrt(1 - tension);
        var adjustedProgress = 1 + linearTension;
        return adjustedProgress * threshold / PullToRefreshDefaults.DragMultiplier;
    }
}

/// <summary>Overlay layout matching PullToRefreshBox's content-and-indicator stack.</summary>
internal sealed class MaterialPullToRefreshBoxPanel : Panel
{
    /// <summary>Defines the indicator's translated top coordinate.</summary>
    public static readonly StyledProperty<double> IndicatorOffsetProperty =
        AvaloniaProperty.Register<MaterialPullToRefreshBoxPanel, double>(nameof(IndicatorOffset));

    static MaterialPullToRefreshBoxPanel() =>
        AffectsArrange<MaterialPullToRefreshBoxPanel>(IndicatorOffsetProperty);

    public double IndicatorOffset
    {
        get => GetValue(IndicatorOffsetProperty);
        set => SetValue(IndicatorOffsetProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0)
            return default;

        Children[0].Measure(availableSize);
        var desired = Children[0].DesiredSize;
        if (Children.Count > 1)
            Children[1].Measure(new Size(desired.Width, double.PositiveInfinity));
        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count > 0)
            Children[0].Arrange(new Rect(finalSize));
        if (Children.Count > 1)
            Children[1].Arrange(new Rect(finalSize));
        return finalSize;
    }
}

/// <summary>Clips only the indicator at the pull box's top edge, as IndicatorBox does upstream.</summary>
internal sealed class MaterialPullToRefreshIndicatorHost : Decorator
{
    public static readonly StyledProperty<double> IndicatorOffsetProperty =
        AvaloniaProperty.Register<MaterialPullToRefreshIndicatorHost, double>(
            nameof(IndicatorOffset));

    static MaterialPullToRefreshIndicatorHost()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialPullToRefreshIndicatorHost>(true);
        AffectsArrange<MaterialPullToRefreshIndicatorHost>(IndicatorOffsetProperty);
    }

    public double IndicatorOffset
    {
        get => GetValue(IndicatorOffsetProperty);
        set => SetValue(IndicatorOffsetProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        return Child?.DesiredSize ?? default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is not { } child)
            return finalSize;
        var desired = child.DesiredSize;
        child.Arrange(new Rect(
            (finalSize.Width - desired.Width) / 2,
            IndicatorOffset,
            desired.Width,
            desired.Height));
        return finalSize;
    }
}

/// <summary>Alpha22 default 40 DIP pull indicator surface, arrow, and refreshing spinner.</summary>
internal sealed class MaterialPullToRefreshIndicator : Control
{
    private readonly MaterialThemeWatcher _theme;
    private readonly MotionGroup _motion;
    private readonly LoopingAnimation _rotation;
    private readonly SpringAnimation _alpha;
    private double _fraction;
    private bool _refreshing;
    private IImmutableBrush _container = new ImmutableSolidColorBrush(Color.FromRgb(236, 230, 240));
    private IImmutableBrush _content = new ImmutableSolidColorBrush(Color.FromRgb(73, 69, 79));
    private Color _shadow = Colors.Black;

    internal bool ShowGlyph { get; set; } = true;

    internal double Elevation { get; set; } = 3;

    internal ComposeShadowShape ParityShadowShape =>
        ComposeShadowShape.Circle(new Rect(Bounds.Size));

    internal ComposeShadowParameters ParityShadowParameters =>
        ComposeShadowParameters.Material(Elevation, _shadow);

    internal bool ParityShadowOccluderDrawn => true;

    public MaterialPullToRefreshIndicator()
    {
        Width = Height = PullToRefreshDefaults.IndicatorContainerSize;
        IsHitTestVisible = false;
        _theme = new MaterialThemeWatcher(this);
        _motion = new MotionGroup(this);
        _rotation = _motion.Add(new LoopingAnimation(TimeSpan.FromMilliseconds(1332)));
        _rotation.Settle();
        _alpha = _motion.AddSpring(.3, new SpringSpec(1, 1600, .01));
        _motion.Ticked += (_, _) => InvalidateVisual();
        _theme.Changed += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    internal void Update(double fraction, bool refreshing)
    {
        _fraction = Math.Max(0, fraction);
        _alpha.AnimateTo(_fraction >= 1 ? 1 : .3, new SpringSpec(1, 1600, .01));
        _motion.Start();
        if (_refreshing != refreshing)
        {
            _refreshing = refreshing;
            if (refreshing)
            {
                _rotation.Resume();
                _motion.Start();
            }
            else
            {
                _rotation.Settle();
            }
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_fraction <= 0 && !_refreshing)
            return;

        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            new CornerRadius(Bounds.Width / 2),
            _container,
            outline: null,
            outlineWidth: 0,
            elevation: Elevation,
            shadowColor: _shadow);

        if (!ShowGlyph)
            return;
        if (_refreshing)
            DrawRefreshing(context);
        else
            DrawArrow(context);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(PullToRefreshDefaults.IndicatorContainerSize, PullToRefreshDefaults.IndicatorContainerSize);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _motion.Start();
    }

    private void DrawRefreshing(DrawingContext context)
    {
        var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = 5.5 + 1.25;
        DrawArc(
            context,
            centre,
            radius,
            -90 + _rotation.Value * 360,
            270,
            _content,
            2.5,
            PenLineCap.Square);
    }

    private void DrawArrow(DrawingContext context)
    {
        var progress = _fraction;
        var adjusted = Math.Max(Math.Min(1, progress) - .4, 0) * 5 / 3;
        var overshoot = Math.Abs(progress) - 1;
        var linearTension = Math.Clamp(overshoot, 0, 2);
        var tension = linearTension - linearTension * linearTension / 4;
        var endTrim = adjusted * .8;
        var rotation = (-.25 + .4 * adjusted + tension) * .5;
        var startAngle = rotation * 360;
        var endAngle = (rotation + endTrim) * 360;
        var scale = Math.Min(1, adjusted);
        var alpha = _alpha.Value;

        var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = 5.5 + 1.25;
        using (context.PushOpacity(alpha))
        using (context.PushTransform(
                   Matrix.CreateTranslation(-centre.X, -centre.Y)
                   * Matrix.CreateRotation(rotation * Math.PI / 180)
                   * Matrix.CreateTranslation(centre.X, centre.Y)))
        {
            DrawArc(
                context,
                centre,
                radius,
                startAngle,
                endAngle - startAngle,
                _content,
                2.5,
                PenLineCap.Square);

            if (scale <= 0)
                return;
            var inset = 10 * scale / 2;
            var origin = new Point(radius + centre.X - inset, centre.Y - 2.5);
            var geometry = new StreamGeometry();
            using (var sink = geometry.Open())
            {
                sink.BeginFigure(origin, false);
                sink.LineTo(origin + new Vector(5 * scale, 5 * scale));
                sink.LineTo(origin + new Vector(10 * scale, 0));
                sink.EndFigure(false);
            }

            using (context.PushTransform(
                       Matrix.CreateTranslation(-centre.X, -centre.Y)
                       * Matrix.CreateRotation((endAngle - 2.5) * Math.PI / 180)
                       * Matrix.CreateTranslation(centre.X, centre.Y)))
            {
                context.DrawGeometry(null, new ImmutablePen(_content, 2.5), geometry);
            }
        }
    }

    private void ApplyTheme()
    {
        var scheme = _theme.Theme.ColorScheme;
        _container = new ImmutableSolidColorBrush(
            scheme?.GetColor(SysColorToken.SurfaceContainerHigh, _theme.IsDark)
            ?? Color.FromRgb(236, 230, 240));
        _content = new ImmutableSolidColorBrush(
            scheme?.GetColor(SysColorToken.OnSurfaceVariant, _theme.IsDark)
            ?? Color.FromRgb(73, 69, 79));
        _shadow = scheme?.GetColor(SysColorToken.Shadow, _theme.IsDark) ?? Colors.Black;
        InvalidateVisual();
    }

    private static void DrawArc(
        DrawingContext context,
        Point centre,
        double radius,
        double startDegrees,
        double sweepDegrees,
        IImmutableBrush brush,
        double thickness,
        PenLineCap cap)
    {
        if (sweepDegrees <= .01)
            return;
        var start = PointOnCircle(centre, radius, startDegrees);
        var end = PointOnCircle(centre, radius, startDegrees + sweepDegrees);
        var geometry = new StreamGeometry();
        using (var sink = geometry.Open())
        {
            sink.BeginFigure(start, false);
            sink.ArcTo(
                end,
                new Size(radius, radius),
                0,
                sweepDegrees > 180,
                SweepDirection.Clockwise);
            sink.EndFigure(false);
        }
        context.DrawGeometry(null, new ImmutablePen(brush, thickness, lineCap: cap), geometry);
    }

    private static Point PointOnCircle(Point centre, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(
            centre.X + Math.Cos(radians) * radius,
            centre.Y + Math.Sin(radians) * radius);
    }
}
