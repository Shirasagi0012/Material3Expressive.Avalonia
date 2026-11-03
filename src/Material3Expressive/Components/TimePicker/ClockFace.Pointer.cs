// Portions translated from fixed AndroidX TimePicker.kt / AnalogTimePickerState / ClockDialNode
// and Core foundation gestures/DragGestureDetector.kt. Apache-2.0 (LICENSE.txt).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

internal sealed partial class ClockFace
{
    private const float RadiansPerHour = MathF.Tau / 12;
    private const float RadiansPerMinute = MathF.Tau / 60;
    private const double QuarterCircle = Math.PI / 2;
    private IPointer? _pointer;
    private bool _dragged, _pointerUpdating;
    private float _lastX, _lastY, _totalX, _totalY, _offsetX, _offsetY;
    private float Density => (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);

    private float EndValueForAnimation(float target)
    {
        var difference = _position - target;
        while (difference > MathF.PI) difference -= MathF.Tau;
        while (difference <= -MathF.PI) difference += MathF.Tau;
        return _position - difference;
    }

    private static int ToHour(float angle) => (int)((angle + (RadiansPerHour / 2 + QuarterCircle)) / RadiansPerHour) % 12;
    private static int ToMinute(float angle) => (int)((angle + (RadiansPerMinute / 2 + QuarterCircle)) / RadiansPerMinute) % 60;
    private static float DialAngle(float y, float x)
    {
        var angle = MathF.Atan2(y, x) - (float)QuarterCircle;
        return angle < 0 ? angle + MathF.Tau : angle;
    }

    private (float X, float Y) Center => ((int)Math.Floor(Bounds.Width * Density + .5) / 2,
        (int)Math.Floor(Bounds.Height * Density + .5) / 2);

    private void MoveSelector(float x, float y)
    {
        if (_controller.Selection != TimeSelection.Hour || !_controller.Is24Hour) return;
        var center = Center;
        var dx = center.X - x;
        var dy = center.Y - y;
        var distance = (float)Math.Sqrt((double)dx * dx + (double)dy * dy);
        var maxDistance = 74f * Density * (float)Math.Floor(Bounds.Width * Density + .5)
                          / (float)Math.Floor(256 * Density + .5);
        _pointerUpdating = true;
        try
        {
            if (_controller.IsPm && distance >= maxDistance) _controller.SetHour(_controller.Value.Hour - 12);
            else if (!_controller.IsPm && distance < maxDistance) _controller.SetHour(_controller.Value.Hour + 12);
        }
        finally { _pointerUpdating = false; }
    }

    private Task<bool> RotateToAsync(AnimationChildScope scope, float value, bool animate, CancellationToken ct, bool snap = false)
    {
        var track = _angle!;
        var motion = Motion;
        return _angleMutex.MutateAsync(async token =>
        {
            // A rejected UserInput mutation must not update the discrete time first.
            _pointerUpdating = true;
            try
            {
                if (_controller.Selection == TimeSelection.Hour)
                {
                    _hourAngle = ToHour(value) % 12 * RadiansPerHour;
                    _controller.SetHour(ToHour(_hourAngle) % 12 + (_controller.IsPm ? 12 : 0));
                }
                else
                {
                    _minuteAngle = ToMinute(value) * RadiansPerMinute;
                    _controller.SetMinute(ToMinute(_minuteAngle));
                }
            }
            finally { _pointerUpdating = false; }
            var offset = value + (float)QuarterCircle;
            if (offset < 0) offset += MathF.Tau;
            if (animate && snap)
                await track.SnapToAsync(EndValueForAnimation(offset), cancellationToken: token);
            else if (animate)
                await track.AnimateToAsync(scope, EndValueForAnimation(offset),
                    new SpringSpec<float>(motion.DampingRatio, motion.Stiffness, (float)motion.VisibilityThreshold), cancellationToken: token);
            else await track.SnapToAsync(offset, cancellationToken: token);
            return true;
        }, MutatePriority.UserInput, ct);
    }

    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsEffectivelyEnabled || _pointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
        if (e.Pointer.Type == PointerType.Touch) SetDialFocusable(false);
        var point = e.GetPosition(this);
        _lastX = _offsetX = (float)point.X * Density;
        _lastY = _offsetY = (float)point.Y * Density;
        _totalX = _totalY = 0;
        _dragged = false;
        _pointer = e.Pointer;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void Moved(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(_pointer, e.Pointer)) return;
        var point = e.GetPosition(this);
        var x = (float)point.X * Density;
        var y = (float)point.Y * Density;
        var dx = x - _lastX;
        var dy = y - _lastY;
        _lastX = x; _lastY = y;
        if (!_dragged)
        {
            _totalX += dx; _totalY += dy;
            var distance = MathF.Sqrt(_totalX * _totalX + _totalY * _totalY);
            var slop = (e.Pointer.Type == PointerType.Mouse ? .125f : 18f) * Density;
            if (distance == 0 || distance < slop) { e.Handled = true; return; }
            dx = _totalX - _totalX / distance * slop;
            dy = _totalY - _totalY / distance * slop;
            _dragged = true;
        }
        if (_scope is { } scope)
        {
            // ClockDialNode keeps the down coordinate plus post-slop deltas, rather than
            // snapping to the pointer position that crossed the threshold.
            _offsetX += dx; _offsetY += dy;
            var offsetX = _offsetX; var offsetY = _offsetY;
            var center = Center;
            _ = scope.Launch(async ct =>
            {
                await RotateToAsync(scope, DialAngle(offsetY - center.Y, offsetX - center.X), false, ct);
                MoveSelector(offsetX, offsetY);
            }, "Clock drag");
        }
        e.Handled = true;
    }

    private void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (!ReferenceEquals(_pointer, e.Pointer)) return;
        var dragged = _dragged;
        CancelPointer();
        if (_scope is { } scope)
        {
            if (dragged)
            {
                // onDragEnd does not consume a new position from the up event.
                if (_controller.AutoSwitchToMinute) _controller.Select(TimeSelection.Minute);
                Animate(TargetAngle, force: true);
            }
            else
            {
                TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
                var point = e.GetPosition(this);
                _ = scope.Launch(ct => TapAsync(scope, point, false, _controller.AutoSwitchToMinute, ct), "Clock tap");
            }
        }
        e.Handled = true;
    }

    private async Task TapAsync(AnimationChildScope scope, Point point, bool snap, bool autoSwitch, CancellationToken ct)
    {
        var x = (float)point.X * Density; var y = (float)point.Y * Density;
        var center = Center;
        var angle = DialAngle(y - center.Y, x - center.X);
        angle = _controller.Selection == TimeSelection.Minute
            ? MathF.Round(angle / RadiansPerMinute / 5) * 5 * RadiansPerMinute
            : MathF.Round(angle / RadiansPerHour) * RadiansPerHour;
        MoveSelector(x, y);
        await RotateToAsync(scope, angle, true, ct, snap);
        if (_controller.Selection == TimeSelection.Hour && autoSwitch)
            await scope.DelayAsync(TimeSpan.FromMilliseconds(100), ct);
        if (autoSwitch) _controller.Select(TimeSelection.Minute);
    }

    private void ActivateLabel(Button label, bool keyboard)
    {
        if (!IsEffectivelyEnabled || _scope is not { } scope) return;
        if (label.TranslatePoint(new Point(label.Bounds.Width / 2, label.Bounds.Height / 2), this) is not { } point) return;
        _ = scope.Launch(ct => TapAsync(scope, point, true, !keyboard && _controller.AutoSwitchToMinute, ct),
            keyboard ? "Clock keyboard selection" : "Clock accessibility selection");
    }

    private void CancelPointer()
    {
        var pointer = _pointer;
        _pointer = null;
        _dragged = false;
        if (pointer?.Captured == this) pointer.Capture(null);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        // detectDragGestures has no onDragCancel callback here: do not settle or advance.
        _pointer = null;
        _dragged = false;
        base.OnPointerCaptureLost(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) CancelPointer();
        else if (change.Property == BoundsProperty && _controller is not null) UpdateLabelSelection();
        else if (change.Property == EffectsMotionProperty && _controller is not null) Update();
        else if (change.Property == SelectorBrushProperty || change.Property == SelectedTextBrushProperty
                 || change.Property == CenterSizeProperty || change.Property == HandleSizeProperty || change.Property == TrackWidthProperty)
        {
            InvalidateVisual(); _selectorOverlay?.InvalidateVisual();
        }
    }
}
