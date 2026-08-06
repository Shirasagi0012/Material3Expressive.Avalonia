using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls.SliderRuntime;
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
    private const double MinimumInteractiveComponentSize = 48;
    // SliderDefaults.Track uses a 2dp inner corner where a track segment faces the thumb.
    // The outer end remains fully rounded (half the 16dp track).
    private const double TrackInsideCornerSize = 2;
    /// <summary>
    /// The axis on which the handle moves. <see cref="Orientation.Vertical" /> adapts Compose
    /// <c>VerticalSlider(topToBottom = true)</c>: the minimum is at the top and the maximum at the
    /// bottom.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialSlider, Orientation>(nameof(Orientation), Orientation.Horizontal);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private SpringAnimation? _handleWidth;
    private SliderTokens _tokens = SliderDefaults.Tokens(ResolvedTheme.Default);
    private SliderColors _colors = SliderDefaults.Colors(ResolvedTheme.Default, isDark: false);

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
        AffectsRender<MaterialSlider>(ValueProperty, MinimumProperty, MaximumProperty, OrientationProperty);
    }

    /// <inheritdoc cref="OrientationProperty" />
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
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
        var length = Orientation == Orientation.Horizontal ? Bounds.Width : Bounds.Height;
        var crossLength = Orientation == Orientation.Horizontal ? Bounds.Height : Bounds.Width;
        if (length <= 0)
            return;

        var enabled = IsEffectivelyEnabled;
        var visuals = SliderDefaults.Visuals(_colors, enabled);
        var trackHeight = _tokens.TrackHeight;
        var trackRadius = trackHeight / 2;
        var handleWidth = HandleWidth.Value;
        var padding = _tokens.HandlePadding;

        // The handle's centre travels between the two ends, inset so the bar stays inside.
        var travel = length - handleWidth;
        var handlePosition = handleWidth / 2 + travel * Fraction;

        // Compose lays out the track between the thumb's two axis-facing edges. Its coordinate
        // origin is therefore half a thumb-width into the control, rather than at Bounds' edge.
        var trackStart = handleWidth / 2;
        var trackEnd = length - handleWidth / 2;
        var activeEnd = handlePosition - handleWidth / 2 - padding;
        var inactiveStart = handlePosition + handleWidth / 2 + padding;
        // SliderImpl rounds the thumb placement to device pixels while retaining the continuous
        // fraction for track drawing. Do the same so a 4dp vertical thumb remains four whole dp
        // at every render scale instead of losing an antialiased bottom row.
        var renderedHandlePosition = SnapToDevicePixel(handlePosition);

        var activeBrush = visuals.ActiveTrackColor;
        var inactiveBrush = visuals.InactiveTrackColor;

        DrawTrack(
            context, activeBrush, trackStart, activeEnd, crossLength / 2, trackHeight,
            trackRadius, TrackInsideCornerSize);

        DrawTrack(
            context, inactiveBrush, inactiveStart, trackEnd, crossLength / 2, trackHeight,
            TrackInsideCornerSize, trackRadius);

        // The stop dot sits inside the far end of the inactive track, and disappears once the
        // handle reaches it.
        if (trackEnd - inactiveStart > trackRadius)
        {
            var radius = _tokens.StopIndicatorSize / 2;
            context.DrawEllipse(
                // SliderDefaults.Track deliberately uses the active track colour here; the
                // similarly named token is for the legacy track overload, not this composable.
                visuals.StopIndicatorColor,
                null,
                PointOnAxis(trackEnd - trackRadius, crossLength / 2),
                radius,
                radius);
        }

        var stateLayerSize = _tokens.HandleHeight;
        _interaction.InteractionArea = RectOnAxis(
            renderedHandlePosition - stateLayerSize / 2,
            crossLength / 2 - stateLayerSize / 2,
            stateLayerSize,
            stateLayerSize);
        var focusRingArea = RectOnAxis(
            renderedHandlePosition - handleWidth / 2,
            crossLength / 2 - _tokens.HandleHeight / 2,
            handleWidth,
            _tokens.HandleHeight).Inflate(4);
        _interaction.FocusRingArea = focusRingArea;
        _interaction.FocusRingCornerRadius =
            new CornerRadius(Math.Min(focusRingArea.Width, focusRingArea.Height) / 2);

        var handleBrush = visuals.HandleColor;
        var handle = RectOnAxis(
            renderedHandlePosition - handleWidth / 2,
            crossLength / 2 - _tokens.HandleHeight / 2,
            handleWidth,
            _tokens.HandleHeight);

        context.DrawRectangle(handleBrush, null, new RoundedRect(handle, new CornerRadius(handleWidth / 2)));
        base.Render(context);
        _interaction.DrawLayers(context, visuals.StateLayerColor, new CornerRadius(stateLayerSize / 2));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var length = Orientation == Orientation.Horizontal
            ? (double.IsInfinity(availableSize.Width) ? 240 : availableSize.Width)
            : (double.IsInfinity(availableSize.Height) ? 240 : availableSize.Height);
        var crossLength = Math.Max(_tokens.HandleHeight, MinimumInteractiveComponentSize);
        return Orientation == Orientation.Horizontal
            ? new Size(length, crossLength)
            : new Size(crossLength, length);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEffectivelyEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        e.Pointer.Capture(this);
        SetValueFrom(AxisPosition(e.GetPosition(this)));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (Equals(e.Pointer.Captured, this))
            SetValueFrom(AxisPosition(e.GetPosition(this)));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? LargeChange : SmallChange;

        var decrement = Orientation == Orientation.Horizontal ? Key.Left : Key.Up;
        var increment = Orientation == Orientation.Horizontal ? Key.Right : Key.Down;
        switch (e.Key)
        {
            case var key when key == decrement:
                Value = Math.Clamp(Value - step, Minimum, Maximum);
                e.Handled = true;
                break;
            case var key when key == increment:
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
        else if (change.Property == OrientationProperty)
            InvalidateMeasure();
    }

    private void SetValueFrom(double position)
    {
        var length = Orientation == Orientation.Horizontal ? Bounds.Width : Bounds.Height;
        var travel = length - HandleWidth.Value;
        if (travel <= 0)
            return;

        var fraction = Math.Clamp((position - HandleWidth.Value / 2) / travel, 0, 1);
        Value = Minimum + (Maximum - Minimum) * fraction;
    }

    private void DrawTrack(
        DrawingContext context,
        IBrush brush,
        double from,
        double to,
        double centre,
        double height,
        double startRadius,
        double endRadius)
    {
        if (to - from < 0.5)
            return;

        var rect = Orientation == Orientation.Horizontal
            ? new Rect(from, centre - height / 2, to - from, height)
            : new Rect(centre - height / 2, from, height, to - from);
        var corners = Orientation == Orientation.Horizontal
            ? new CornerRadius(startRadius, endRadius, endRadius, startRadius)
            : new CornerRadius(startRadius, startRadius, endRadius, endRadius);
        context.DrawRectangle(brush, null, new RoundedRect(rect, corners));
    }

    private Point PointOnAxis(double axis, double cross) =>
        Orientation == Orientation.Horizontal ? new Point(axis, cross) : new Point(cross, axis);

    private Rect RectOnAxis(double axis, double cross, double axisLength, double crossLength) =>
        Orientation == Orientation.Horizontal
            ? new Rect(axis, cross, axisLength, crossLength)
            : new Rect(cross, axis, crossLength, axisLength);

    private double AxisPosition(Point point) => Orientation == Orientation.Horizontal ? point.X : point.Y;

    private double SnapToDevicePixel(double value)
    {
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        return Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
    }

    private void ApplyTokens()
    {
        _tokens = SliderDefaults.Tokens(_theme.Theme);
        _colors = SliderDefaults.Colors(_theme.Theme, _theme.IsDark);

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
