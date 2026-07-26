using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 Expressive slider.
/// </summary>
/// <remarks>
/// Expressive redrew this one substantially: the round thumb became a 4dp-wide bar standing taller
/// than the track, the track thickened to 16dp, and the two halves are separated by a gap around
/// the handle rather than meeting under it. The handle also narrows to 2dp while pressed, so the
/// value beneath it stays visible.
/// <para>
/// Derives from <see cref="RangeBase" />, so <c>Minimum</c>, <c>Maximum</c>, <c>Value</c> and the
/// small/large change steps are the stock ones and behave as they do on every other range control.
/// </para>
/// </remarks>
public class MaterialSlider : RangeBase
{
    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private SpringAnimation? _handleWidth;
    private SliderTokens _tokens = SliderTokens.Default;
    private SliderColors _colors = SliderTokens.Default.Resolve(null, false);

    public MaterialSlider()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => UpdateHandleWidth();

        Focusable = true;
        ApplyTokens();
    }

    static MaterialSlider()
    {
        AffectsRender<MaterialSlider>(ValueProperty, MinimumProperty, MaximumProperty);
    }

    /// <summary>Where the handle sits, as a fraction of the track.</summary>
    protected double Fraction
    {
        get
        {
            var range = Maximum - Minimum;
            return range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
        }
    }

    private SpringAnimation HandleWidth =>
        _handleWidth ??= _interaction.Motion.AddSpring(_tokens.HandleWidth, _theme.Spring(MotionRole.FastEffects));

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var centreY = Bounds.Height / 2;
        if (width <= 0)
            return;

        var enabled = IsEffectivelyEnabled;
        var trackHeight = _tokens.TrackHeight;
        var trackRadius = trackHeight / 2;
        var handleWidth = HandleWidth.Value;
        var padding = _tokens.HandlePadding;

        // The handle's centre travels between the two ends, inset so the bar stays inside.
        var travel = width - handleWidth;
        var handleX = handleWidth / 2 + travel * Fraction;

        var activeEnd = handleX - handleWidth / 2 - padding;
        var inactiveStart = handleX + handleWidth / 2 + padding;

        var activeBrush = enabled ? _colors.ActiveTrackColor : _colors.DisabledActiveTrackColor;
        var inactiveBrush = enabled ? _colors.InactiveTrackColor : _colors.DisabledInactiveTrackColor;

        DrawTrack(context, activeBrush, 0, activeEnd, centreY, trackHeight, trackRadius);

        var stopRoom = _tokens.StopIndicatorSize + _tokens.StopIndicatorTrailingSpace;
        DrawTrack(context, inactiveBrush, inactiveStart, width, centreY, trackHeight, trackRadius);

        // The stop dot sits inside the far end of the inactive track, and disappears once the
        // handle reaches it.
        if (width - inactiveStart > stopRoom)
        {
            var radius = _tokens.StopIndicatorSize / 2;
            context.DrawEllipse(
                _colors.StopIndicatorColor,
                null,
                new Point(width - _tokens.StopIndicatorTrailingSpace - radius, centreY),
                radius,
                radius);
        }

        var stateLayerSize = _tokens.HandleHeight;
        _interaction.InteractionArea = new Rect(
            handleX - stateLayerSize / 2, centreY - stateLayerSize / 2, stateLayerSize, stateLayerSize);
        _interaction.DrawLayers(context, _colors.StateLayerColor, new CornerRadius(stateLayerSize / 2));

        var handleBrush = enabled ? _colors.HandleColor : _colors.DisabledHandleColor;
        var handle = new Rect(
            handleX - handleWidth / 2, centreY - _tokens.HandleHeight / 2, handleWidth, _tokens.HandleHeight);

        context.DrawRectangle(handleBrush, null, new RoundedRect(handle, new CornerRadius(handleWidth / 2)));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 240 : availableSize.Width;
        return new Size(width, _tokens.HandleHeight);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEffectivelyEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        e.Pointer.Capture(this);
        SetValueFrom(e.GetPosition(this).X);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (Equals(e.Pointer.Captured, this))
            SetValueFrom(e.GetPosition(this).X);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? LargeChange : SmallChange;

        switch (e.Key)
        {
            case Key.Left or Key.Down:
                Value = Math.Clamp(Value - step, Minimum, Maximum);
                e.Handled = true;
                break;
            case Key.Right or Key.Up:
                Value = Math.Clamp(Value + step, Minimum, Maximum);
                e.Handled = true;
                break;
            case Key.Home:
                Value = Minimum;
                e.Handled = true;
                break;
            case Key.End:
                Value = Maximum;
                e.Handled = true;
                break;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsEffectivelyEnabledProperty)
            ApplyTokens();
    }

    private void SetValueFrom(double x)
    {
        var travel = Bounds.Width - HandleWidth.Value;
        if (travel <= 0)
            return;

        var fraction = Math.Clamp((x - HandleWidth.Value / 2) / travel, 0, 1);
        Value = Minimum + (Maximum - Minimum) * fraction;
    }

    private static void DrawTrack(
        DrawingContext context, IBrush brush, double from, double to, double centreY, double height, double radius)
    {
        if (to - from < 0.5)
            return;

        context.DrawRectangle(
            brush,
            null,
            new RoundedRect(new Rect(from, centreY - height / 2, to - from, height), new CornerRadius(radius)));
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.SliderTokens;
        _colors = _theme.Theme.GetSliderColors(_theme.IsDark);

        UpdateHandleWidth();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void UpdateHandleWidth()
    {
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? _tokens.PressedHandleWidth
            : _tokens.HandleWidth;

        HandleWidth.AnimateTo(target, _theme.Spring(MotionRole.FastEffects));
        _interaction.Motion.Start();
        InvalidateVisual();
    }
}
