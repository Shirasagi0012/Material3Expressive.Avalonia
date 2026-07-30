// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SwipeToDismissBox.kt.
// Compose logical anchors and thresholds are preserved. Delete / Shift+Delete / Escape and the
// Invoke automation pattern are intentional Avalonia desktop adaptations under open decision D003.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>The logical anchors of <see cref="MaterialSwipeToDismissBox" />.</summary>
public enum SwipeToDismissBoxValue
{
    /// <summary>Dismissed in the reading direction.</summary>
    StartToEnd,

    /// <summary>Dismissed in the reverse reading direction.</summary>
    EndToStart,

    /// <summary>Resting at the origin.</summary>
    Settled
}

/// <summary>Default gesture thresholds for <see cref="MaterialSwipeToDismissBox" />.</summary>
public static class SwipeToDismissBoxDefaults
{
    /// <summary>Default distance from the origin that selects a dismissal anchor.</summary>
    public const double PositionalThreshold = 56;

    /// <summary>Default logical fling velocity that selects a dismissal anchor.</summary>
    public const double VelocityThreshold = 125;

    internal const double PointerSlop = 4;
}

/// <summary>
/// Recoverable logical state for a swipe-to-dismiss surface.
/// </summary>
/// <remarks>
/// The initial value is the persistence boundary: applications save <see cref="CurrentValue" />
/// and pass it back to this constructor. Animation remains control-owned so state replacement and
/// commands can retarget from the currently visible offset.
/// </remarks>
public sealed class SwipeToDismissBoxState : INotifyPropertyChanged
{
    private SwipeToDismissBoxValue _currentValue;
    private SwipeToDismissBoxValue _targetValue;
    private SwipeToDismissBoxValue _settledValue;
    private double _offset = double.NaN;
    private double _progress = 1;

    /// <summary>Creates state at one of the three logical anchors.</summary>
    /// <param name="initialValue">The anchor restored by the application.</param>
    /// <param name="positionalThreshold">
    /// Returns the positive distance from the origin that selects dismissal.
    /// </param>
    public SwipeToDismissBoxState(
        SwipeToDismissBoxValue initialValue = SwipeToDismissBoxValue.Settled,
        Func<double, double>? positionalThreshold = null)
    {
        ValidateValue(initialValue, nameof(initialValue));
        _currentValue = _targetValue = _settledValue = initialValue;
        PositionalThreshold = positionalThreshold
            ?? (static _ => SwipeToDismissBoxDefaults.PositionalThreshold);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    internal event EventHandler<SwipeTransitionRequestedEventArgs>? TransitionRequested;

    /// <summary>The anchor closest to the current interaction.</summary>
    public SwipeToDismissBoxValue CurrentValue => _currentValue;

    /// <summary>The anchor the active drag or animation is heading toward.</summary>
    public SwipeToDismissBoxValue TargetValue => _targetValue;

    /// <summary>The last anchor at which motion completed.</summary>
    public SwipeToDismissBoxValue SettledValue => _settledValue;

    /// <summary>The current logical offset: positive is StartToEnd and negative is EndToStart.</summary>
    public double Offset => _offset;

    /// <summary>Normalized progress between the current transition's anchors.</summary>
    public double Progress => _progress;

    /// <summary>The logical direction exposed by the current offset.</summary>
    public SwipeToDismissBoxValue DismissDirection =>
        double.IsNaN(_offset) || Math.Abs(_offset) < double.Epsilon
            ? SwipeToDismissBoxValue.Settled
            : _offset > 0
                ? SwipeToDismissBoxValue.StartToEnd
                : SwipeToDismissBoxValue.EndToStart;

    /// <summary>Calculates the positive positional threshold for a given anchor distance.</summary>
    public Func<double, double> PositionalThreshold { get; }

    /// <summary>Returns the initialized logical offset.</summary>
    /// <exception cref="InvalidOperationException">The state has not been attached and measured.</exception>
    public double RequireOffset() =>
        double.IsNaN(_offset)
            ? throw new InvalidOperationException("The swipe anchors have not been initialized.")
            : _offset;

    /// <summary>Moves immediately to an anchor.</summary>
    public void SnapTo(SwipeToDismissBoxValue targetValue) =>
        RequestTransition(targetValue, snap: true);

    /// <summary>Animates back to <see cref="SwipeToDismissBoxValue.Settled" />.</summary>
    public void Reset() =>
        RequestTransition(SwipeToDismissBoxValue.Settled, snap: false);

    /// <summary>Animates to a dismissal anchor.</summary>
    public void Dismiss(SwipeToDismissBoxValue direction)
    {
        if (direction == SwipeToDismissBoxValue.Settled)
            throw new ArgumentException("Dismiss requires a dismissal direction.", nameof(direction));
        RequestTransition(direction, snap: false);
    }

    internal double GetThreshold(double totalDistance)
    {
        var threshold = PositionalThreshold(totalDistance);
        if (!double.IsFinite(threshold) || threshold <= 0)
        {
            throw new InvalidOperationException(
                "SwipeToDismissBoxState.PositionalThreshold must return a positive finite value.");
        }

        return Math.Min(threshold, Math.Max(0, totalDistance));
    }

    internal void RequestTransition(SwipeToDismissBoxValue value, bool snap)
    {
        ValidateValue(value, nameof(value));
        SetTargetValue(value);
        var args = new SwipeTransitionRequestedEventArgs(value, snap);
        TransitionRequested?.Invoke(this, args);
        if (!args.Handled)
            SetAtAnchor(value, AnchorOffset(value, 0));
    }

    internal void UpdateInteraction(
        double offset,
        double totalDistance,
        SwipeToDismissBoxValue currentValue,
        SwipeToDismissBoxValue targetValue,
        double progress)
    {
        SetOffset(offset);
        SetCurrentValue(currentValue);
        SetTargetValue(targetValue);
        SetProgress(progress);
    }

    internal void SetAtAnchor(SwipeToDismissBoxValue value, double offset)
    {
        SetOffset(offset);
        SetTargetValue(value);
        SetCurrentValue(value);
        SetSettledValue(value);
        SetProgress(1);
    }

    private static double AnchorOffset(SwipeToDismissBoxValue value, double width) =>
        value switch
        {
            SwipeToDismissBoxValue.StartToEnd => width,
            SwipeToDismissBoxValue.EndToStart => -width,
            _ => 0
        };

    private void SetCurrentValue(SwipeToDismissBoxValue value) =>
        SetField(ref _currentValue, value, nameof(CurrentValue));

    private void SetTargetValue(SwipeToDismissBoxValue value) =>
        SetField(ref _targetValue, value, nameof(TargetValue));

    private void SetSettledValue(SwipeToDismissBoxValue value) =>
        SetField(ref _settledValue, value, nameof(SettledValue));

    private void SetOffset(double value)
    {
        if ((double.IsNaN(_offset) && double.IsNaN(value)) || _offset.Equals(value))
            return;
        _offset = value;
        PropertyChanged?.Invoke(this, new(nameof(Offset)));
        PropertyChanged?.Invoke(this, new(nameof(DismissDirection)));
    }

    private void SetProgress(double value)
    {
        value = Math.Clamp(value, 0, 1);
        if (Math.Abs(_progress - value) < 0.000001)
            return;
        _progress = value;
        PropertyChanged?.Invoke(this, new(nameof(Progress)));
    }

    private void SetField(
        ref SwipeToDismissBoxValue field,
        SwipeToDismissBoxValue value,
        string propertyName)
    {
        if (field == value)
            return;
        field = value;
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    private static void ValidateValue(SwipeToDismissBoxValue value, string parameterName)
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(parameterName);
    }
}

/// <summary>Event raised once motion settles at a dismissal anchor.</summary>
public sealed class SwipeDismissedEventArgs(SwipeToDismissBoxValue direction) : EventArgs
{
    /// <summary>The logical dismissal direction.</summary>
    public SwipeToDismissBoxValue Direction { get; } = direction;
}

internal sealed class SwipeTransitionRequestedEventArgs(
    SwipeToDismissBoxValue targetValue,
    bool snap) : EventArgs
{
    public SwipeToDismissBoxValue TargetValue { get; } = targetValue;

    public bool Snap { get; } = snap;

    public bool Handled { get; set; }
}

/// <summary>
/// A foreground content surface that exposes background content while it is swiped away.
/// </summary>
public sealed class MaterialSwipeToDismissBox : ContentControl
{
    /// <summary>Defines the content stacked behind the dismissible foreground.</summary>
    public static readonly StyledProperty<object?> BackgroundContentProperty =
        AvaloniaProperty.Register<MaterialSwipeToDismissBox, object?>(nameof(BackgroundContent));

    /// <summary>Defines whether the reading-direction anchor exists.</summary>
    public static readonly StyledProperty<bool> EnableDismissFromStartToEndProperty =
        AvaloniaProperty.Register<MaterialSwipeToDismissBox, bool>(
            nameof(EnableDismissFromStartToEnd), true);

    /// <summary>Defines whether the reverse-reading-direction anchor exists.</summary>
    public static readonly StyledProperty<bool> EnableDismissFromEndToStartProperty =
        AvaloniaProperty.Register<MaterialSwipeToDismissBox, bool>(
            nameof(EnableDismissFromEndToStart), true);

    /// <summary>Defines whether pointer gestures may manipulate the foreground.</summary>
    public static readonly StyledProperty<bool> GesturesEnabledProperty =
        AvaloniaProperty.Register<MaterialSwipeToDismissBox, bool>(nameof(GesturesEnabled), true);

    /// <summary>Defines the replaceable, non-null state owner.</summary>
    public static readonly DirectProperty<MaterialSwipeToDismissBox, SwipeToDismissBoxState>
        StateProperty =
            AvaloniaProperty.RegisterDirect<MaterialSwipeToDismissBox, SwipeToDismissBoxState>(
                nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));

    /// <summary>Defines the foreground's physical offset after RTL mapping.</summary>
    public static readonly DirectProperty<MaterialSwipeToDismissBox, double> ContentOffsetProperty =
        AvaloniaProperty.RegisterDirect<MaterialSwipeToDismissBox, double>(
            nameof(ContentOffset), owner => owner._contentOffset);

    private readonly MotionGroup _motion;
    private SwipeToDismissBoxState _state = new();
    private TweenAnimation? _offsetMotion;
    private double _contentOffset;
    private double _anchorWidth;
    private bool _dragging;
    private bool _pointerArmed;
    private Point _pointerStart;
    private double _dragStartOffset;
    private double _lastLogicalOffset;
    private ulong _lastPointerTimestamp;
    private double _logicalVelocity;
    private bool _dismissNotificationDelivered;
    private IPointer? _capturedPointer;

    /// <summary>Creates a swipe-to-dismiss surface with per-instance state.</summary>
    public MaterialSwipeToDismissBox()
    {
        Focusable = true;
        _motion = new MotionGroup(this);
        _motion.Ticked += OnMotionTick;
        AttachState(_state);
    }

    /// <summary>Raised once after motion settles at a dismissal anchor.</summary>
    public event EventHandler<SwipeDismissedEventArgs>? Dismissed;

    /// <summary>The background stacked behind the dismissible foreground.</summary>
    public object? BackgroundContent
    {
        get => GetValue(BackgroundContentProperty);
        set => SetValue(BackgroundContentProperty, value);
    }

    /// <summary>Whether dismissal in the reading direction is enabled.</summary>
    public bool EnableDismissFromStartToEnd
    {
        get => GetValue(EnableDismissFromStartToEndProperty);
        set => SetValue(EnableDismissFromStartToEndProperty, value);
    }

    /// <summary>Whether dismissal opposite the reading direction is enabled.</summary>
    public bool EnableDismissFromEndToStart
    {
        get => GetValue(EnableDismissFromEndToStartProperty);
        set => SetValue(EnableDismissFromEndToStartProperty, value);
    }

    /// <summary>Whether pointer gestures are enabled. Commands and automation remain available.</summary>
    public bool GesturesEnabled
    {
        get => GetValue(GesturesEnabledProperty);
        set => SetValue(GesturesEnabledProperty, value);
    }

    /// <summary>Gets or replaces the state owner. The getter is always non-null.</summary>
    public SwipeToDismissBoxState State
    {
        get => _state;
        set => SetState(value);
    }

    /// <summary>The foreground's current physical X offset.</summary>
    public double ContentOffset => _contentOffset;

    /// <summary>Replaces the current state with fresh settled state.</summary>
    public void ResetState() => SetState(null);

    internal double MotionTargetOffset => _offsetMotion?.Target ?? State.Offset;

    internal bool BeginDrag()
    {
        if (!GesturesEnabled
            || State.SettledValue != SwipeToDismissBoxValue.Settled
            || (!EnableDismissFromStartToEnd && !EnableDismissFromEndToStart))
        {
            return false;
        }

        EnsureMotion();
        _offsetMotion!.SnapTo(double.IsNaN(State.Offset) ? 0 : State.Offset);
        _dragging = true;
        _dismissNotificationDelivered = false;
        return true;
    }

    internal double DragBy(double logicalDelta)
    {
        if (!_dragging || !double.IsFinite(logicalDelta))
            return State.Offset;

        return SetDragOffset((double.IsNaN(State.Offset) ? 0 : State.Offset) + logicalDelta);
    }

    internal void EndDrag(double logicalVelocity = 0)
    {
        if (!_dragging)
            return;

        _dragging = false;
        var offset = double.IsNaN(State.Offset) ? 0 : State.Offset;
        var direction = DirectionForOffset(offset);
        var crossesPosition =
            direction != SwipeToDismissBoxValue.Settled
            && Math.Abs(offset) >= State.GetThreshold(_anchorWidth);
        var crossesVelocity =
            direction != SwipeToDismissBoxValue.Settled
            && Math.Sign(logicalVelocity) == Math.Sign(offset)
            && Math.Abs(logicalVelocity) >= SwipeToDismissBoxDefaults.VelocityThreshold;
        var target = crossesPosition || crossesVelocity
            ? direction
            : SwipeToDismissBoxValue.Settled;

        State.RequestTransition(target, snap: false);
    }

    internal void DismissFromAutomation()
    {
        if (EnableDismissFromEndToStart)
            State.Dismiss(SwipeToDismissBoxValue.EndToStart);
        else if (EnableDismissFromStartToEnd)
            State.Dismiss(SwipeToDismissBoxValue.StartToEnd);
    }

    internal bool HasDismissAction =>
        EnableDismissFromStartToEnd || EnableDismissFromEndToStart;

    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateAnchorWidth(finalSize.Width);
        return base.ArrangeOverride(finalSize);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || !GesturesEnabled
            || State.SettledValue != SwipeToDismissBoxValue.Settled)
        {
            return;
        }

        _pointerArmed = true;
        _pointerStart = e.GetPosition(this);
        _dragStartOffset = double.IsNaN(State.Offset) ? 0 : State.Offset;
        _lastLogicalOffset = _dragStartOffset;
        _lastPointerTimestamp = e.Timestamp;
        _logicalVelocity = 0;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_pointerArmed)
            return;

        var point = e.GetPosition(this);
        var physicalDelta = point.X - _pointerStart.X;
        var verticalDelta = point.Y - _pointerStart.Y;
        if (!_dragging)
        {
            if (Math.Max(Math.Abs(physicalDelta), Math.Abs(verticalDelta))
                < SwipeToDismissBoxDefaults.PointerSlop)
            {
                return;
            }

            if (Math.Abs(verticalDelta) > Math.Abs(physicalDelta))
            {
                _pointerArmed = false;
                return;
            }

            var logicalDelta = ToLogicalOffset(physicalDelta);
            if (!IsDirectionEnabled(DirectionForOffset(logicalDelta)) || !BeginDrag())
            {
                _pointerArmed = false;
                return;
            }

            e.Pointer.Capture(this);
            _capturedPointer = e.Pointer;
        }

        var logicalOffset = SetDragOffset(_dragStartOffset + ToLogicalOffset(physicalDelta));
        if (e.Timestamp > _lastPointerTimestamp)
        {
            var elapsedSeconds = (e.Timestamp - _lastPointerTimestamp) / 1000d;
            if (elapsedSeconds > 0)
                _logicalVelocity = (logicalOffset - _lastLogicalOffset) / elapsedSeconds;
        }
        _lastLogicalOffset = logicalOffset;
        _lastPointerTimestamp = e.Timestamp;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var wasDragging = _dragging;
        _pointerArmed = false;
        if (!wasDragging)
            return;

        EndDrag(_logicalVelocity);
        e.Pointer.Capture(null);
        _capturedPointer = null;
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _capturedPointer = null;
        _pointerArmed = false;
        if (_dragging)
            EndDrag(_logicalVelocity);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape
            && (State.TargetValue != SwipeToDismissBoxValue.Settled
                || Math.Abs(double.IsNaN(State.Offset) ? 0 : State.Offset) > double.Epsilon))
        {
            State.Reset();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && HasDismissAction)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (EnableDismissFromStartToEnd)
                    State.Dismiss(SwipeToDismissBoxValue.StartToEnd);
                else
                    State.Dismiss(SwipeToDismissBoxValue.EndToStart);
            }
            else
            {
                DismissFromAutomation();
            }

            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == EnableDismissFromStartToEndProperty
            || change.Property == EnableDismissFromEndToStartProperty)
        {
            CoerceRemovedAnchor();
        }
        else if (change.Property == FlowDirectionProperty)
        {
            UpdateContentOffset();
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialSwipeToDismissBoxAutomationPeer(this);

    private void SetState(SwipeToDismissBoxState? value)
    {
        var next = value ?? new SwipeToDismissBoxState();
        if (ReferenceEquals(_state, next))
            return;

        CancelPointerInteraction();
        DetachState(_state);
        SetAndRaise(StateProperty, ref _state, next);
        AttachState(next);
        _dismissNotificationDelivered = false;
        SnapVisualToState(notifyDismissed: true);
    }

    private void AttachState(SwipeToDismissBoxState state)
    {
        state.TransitionRequested += OnTransitionRequested;
        state.PropertyChanged += OnStatePropertyChanged;
    }

    private void DetachState(SwipeToDismissBoxState state)
    {
        state.TransitionRequested -= OnTransitionRequested;
        state.PropertyChanged -= OnStatePropertyChanged;
    }

    private void OnTransitionRequested(object? sender, SwipeTransitionRequestedEventArgs e)
    {
        if (!ReferenceEquals(sender, State))
            return;

        e.Handled = true;
        CancelPointerInteraction();
        var target = IsDirectionEnabled(e.TargetValue)
            ? e.TargetValue
            : SwipeToDismissBoxValue.Settled;
        var targetOffset = AnchorOffset(target);
        EnsureMotion();

        var shouldSnap = e.Snap || TopLevel.GetTopLevel(this) is null;
        if (shouldSnap)
        {
            _offsetMotion!.SnapTo(targetOffset);
            State.SetAtAnchor(target, targetOffset);
            UpdateContentOffset();
            CompleteDismissNotification(target);
            return;
        }

        _dismissNotificationDelivered =
            target == SwipeToDismissBoxValue.Settled ? false : _dismissNotificationDelivered;
        _offsetMotion!.AnimateTo(
            targetOffset,
            TimeSpan.FromMilliseconds(300),
            Easings.FastOutSlowIn);
        UpdateStateFromMotion(target);
        _motion.Start();
    }

    private void OnMotionTick(object? sender, EventArgs e)
    {
        if (_offsetMotion is null)
            return;

        var target = ValueForAnchor(_offsetMotion.Target);
        UpdateStateFromMotion(target);
        if (!_offsetMotion.IsRunning)
        {
            State.SetAtAnchor(target, _offsetMotion.Value);
            CompleteDismissNotification(target);
        }
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SwipeToDismissBoxState.Offset))
            UpdateContentOffset();
    }

    private double SetDragOffset(double logicalOffset)
    {
        var minimum = EnableDismissFromEndToStart ? -_anchorWidth : 0;
        var maximum = EnableDismissFromStartToEnd ? _anchorWidth : 0;
        logicalOffset = Math.Clamp(logicalOffset, minimum, maximum);
        EnsureMotion();
        _offsetMotion!.SnapTo(logicalOffset);

        var direction = DirectionForOffset(logicalOffset);
        var target =
            direction != SwipeToDismissBoxValue.Settled
            && Math.Abs(logicalOffset) >= State.GetThreshold(_anchorWidth)
                ? direction
                : SwipeToDismissBoxValue.Settled;
        State.UpdateInteraction(
            logicalOffset,
            _anchorWidth,
            SwipeToDismissBoxValue.Settled,
            target,
            _anchorWidth <= 0 ? 0 : Math.Abs(logicalOffset) / _anchorWidth);
        UpdateContentOffset();
        return logicalOffset;
    }

    private void UpdateStateFromMotion(SwipeToDismissBoxValue target)
    {
        if (_offsetMotion is null)
            return;

        var start = AnchorOffset(State.SettledValue);
        var finish = AnchorOffset(target);
        var travel = Math.Abs(finish - start);
        var progress = travel <= double.Epsilon
            ? 1
            : Math.Abs(_offsetMotion.Value - start) / travel;
        var current = progress >= 1
            ? target
            : State.SettledValue;
        State.UpdateInteraction(
            _offsetMotion.Value,
            _anchorWidth,
            current,
            target,
            progress);
        UpdateContentOffset();
    }

    private void UpdateAnchorWidth(double width)
    {
        width = Math.Max(0, width);
        if (Math.Abs(_anchorWidth - width) < 0.000001)
            return;

        var oldWidth = _anchorWidth;
        _anchorWidth = width;
        EnsureMotion();
        if (_dragging && oldWidth > 0)
        {
            _offsetMotion!.SnapTo(Math.Clamp(
                _offsetMotion.Value / oldWidth * width,
                EnableDismissFromEndToStart ? -width : 0,
                EnableDismissFromStartToEnd ? width : 0));
            UpdateStateFromMotion(State.TargetValue);
        }
        else if (!_offsetMotion!.IsRunning)
        {
            SnapVisualToState(notifyDismissed: true);
        }
        else
        {
            _offsetMotion.AnimateTo(
                AnchorOffset(State.TargetValue),
                TimeSpan.FromMilliseconds(300),
                Easings.FastOutSlowIn);
        }
    }

    private void SnapVisualToState(bool notifyDismissed)
    {
        var value = IsDirectionEnabled(State.CurrentValue)
            ? State.CurrentValue
            : SwipeToDismissBoxValue.Settled;
        var offset = AnchorOffset(value);
        EnsureMotion();
        _offsetMotion!.SnapTo(offset);
        State.SetAtAnchor(value, offset);
        UpdateContentOffset();
        if (notifyDismissed)
            CompleteDismissNotification(value);
    }

    private void CoerceRemovedAnchor()
    {
        if (!IsDirectionEnabled(State.CurrentValue)
            || !IsDirectionEnabled(State.TargetValue)
            || !IsDirectionEnabled(State.SettledValue))
        {
            State.SnapTo(SwipeToDismissBoxValue.Settled);
        }
    }

    private void CompleteDismissNotification(SwipeToDismissBoxValue value)
    {
        if (value == SwipeToDismissBoxValue.Settled)
        {
            _dismissNotificationDelivered = false;
            return;
        }

        if (_dismissNotificationDelivered)
            return;
        _dismissNotificationDelivered = true;
        Dismissed?.Invoke(this, new SwipeDismissedEventArgs(value));
    }

    private void EnsureMotion()
    {
        _offsetMotion ??= _motion.AddTween(
            double.IsNaN(State.Offset) ? 0 : State.Offset,
            TimeSpan.FromMilliseconds(300),
            Easings.FastOutSlowIn);
    }

    private void CancelPointerInteraction()
    {
        _pointerArmed = false;
        _dragging = false;
        _capturedPointer?.Capture(null);
        _capturedPointer = null;
    }

    private bool IsDirectionEnabled(SwipeToDismissBoxValue value) =>
        value switch
        {
            SwipeToDismissBoxValue.StartToEnd => EnableDismissFromStartToEnd,
            SwipeToDismissBoxValue.EndToStart => EnableDismissFromEndToStart,
            _ => true
        };

    private double AnchorOffset(SwipeToDismissBoxValue value) =>
        value switch
        {
            SwipeToDismissBoxValue.StartToEnd => _anchorWidth,
            SwipeToDismissBoxValue.EndToStart => -_anchorWidth,
            _ => 0
        };

    private SwipeToDismissBoxValue ValueForAnchor(double offset)
    {
        if (_anchorWidth <= 0 || Math.Abs(offset) < double.Epsilon)
            return SwipeToDismissBoxValue.Settled;
        return offset > 0
            ? SwipeToDismissBoxValue.StartToEnd
            : SwipeToDismissBoxValue.EndToStart;
    }

    private static SwipeToDismissBoxValue DirectionForOffset(double offset) =>
        Math.Abs(offset) < double.Epsilon
            ? SwipeToDismissBoxValue.Settled
            : offset > 0
                ? SwipeToDismissBoxValue.StartToEnd
                : SwipeToDismissBoxValue.EndToStart;

    private double ToLogicalOffset(double physicalOffset) =>
        FlowDirection == FlowDirection.RightToLeft ? -physicalOffset : physicalOffset;

    private void UpdateContentOffset()
    {
        var logicalOffset = double.IsNaN(State.Offset) ? 0 : State.Offset;
        var physicalOffset = FlowDirection == FlowDirection.RightToLeft
            ? -logicalOffset
            : logicalOffset;
        if (Math.Abs(_contentOffset - physicalOffset) < 0.000001)
            return;
        SetAndRaise(ContentOffsetProperty, ref _contentOffset, physicalOffset);
    }
}

internal sealed class MaterialSwipeToDismissBoxPanel : Panel
{
    public static readonly StyledProperty<double> ContentOffsetProperty =
        AvaloniaProperty.Register<MaterialSwipeToDismissBoxPanel, double>(nameof(ContentOffset));

    static MaterialSwipeToDismissBoxPanel() =>
        AffectsArrange<MaterialSwipeToDismissBoxPanel>(ContentOffsetProperty);

    public double ContentOffset
    {
        get => GetValue(ContentOffsetProperty);
        set => SetValue(ContentOffsetProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0)
            return default;

        var content = Children.Count > 1 ? Children[1] : Children[0];
        content.Measure(availableSize);
        var desired = content.DesiredSize;
        if (Children.Count > 1)
            Children[0].Measure(desired);
        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count > 1)
            Children[0].Arrange(new Rect(finalSize));
        if (Children.Count > 0)
        {
            var content = Children.Count > 1 ? Children[1] : Children[0];
            content.Arrange(new Rect(ContentOffset, 0, finalSize.Width, finalSize.Height));
        }
        return finalSize;
    }
}
