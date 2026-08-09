using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>A Material 3 Expressive two-thumb range slider.</summary>
/// <remarks>
/// This is the Avalonia/XAML adaptation of Compose <c>RangeSlider</c>. Its two public values are
/// intentionally separate instead of overloading <see cref="Avalonia.Controls.Primitives.RangeBase.Value"/>:
/// <see cref="StartValue"/> and <see cref="EndValue"/> are independently bindable, clamped to the
/// configured range, and never cross. Pointer input selects the nearer thumb; keyboard input
/// operates that selected thumb. Tab moves from the Start endpoint to the End endpoint before
/// leaving the control; Shift+Tab traverses the two endpoints in reverse.
/// </remarks>
public sealed class MaterialRangeSlider : Control, IMaterialThemeConsumer
{
    private const double MinimumInteractiveComponentSize = 48;
    private const double TrackInsideCornerSize = 2;
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, double>(nameof(Minimum));
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, double>(nameof(Maximum), 100d);
    public static readonly StyledProperty<double> StartValueProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, double>(nameof(StartValue));
    public static readonly StyledProperty<double> EndValueProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, double>(nameof(EndValue), 100d);
    public static readonly StyledProperty<double> SmallChangeProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, double>(nameof(SmallChange), 1d);
    public static readonly StyledProperty<double> LargeChangeProperty =
        AvaloniaProperty.Register<MaterialRangeSlider, double>(nameof(LargeChange), 10d);

    private readonly MaterialElementContext _theme;
    private readonly MaterialInteractionController _interaction;
    private SliderMetrics _tokens = SliderDefaults.Metrics();
    private SliderColors _colors = SliderDefaults.Colors(ResolvedTheme.Default, isDark: false);
    private bool _updating;
    private bool _startActive = true;

    public MaterialRangeSlider()
    {
        _theme = MaterialElementContext.GetOrCreate(this);
        _interaction = MaterialElementContext.GetOrCreate(this).Interaction;
        Focusable = true;
        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    static MaterialRangeSlider()
    {
        AffectsRender<MaterialRangeSlider>(
            MinimumProperty, MaximumProperty, StartValueProperty, EndValueProperty,
            SmallChangeProperty, LargeChangeProperty);
    }

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double StartValue { get => GetValue(StartValueProperty); set => SetValue(StartValueProperty, value); }
    public double EndValue { get => GetValue(EndValueProperty); set => SetValue(EndValueProperty, value); }
    public double SmallChange { get => GetValue(SmallChangeProperty); set => SetValue(SmallChangeProperty, value); }
    public double LargeChange { get => GetValue(LargeChangeProperty); set => SetValue(LargeChangeProperty, value); }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        if (width <= 0)
            return;
        var center = Bounds.Height / 2;
        var handleWidth = _tokens.HandleWidth;
        var start = PositionFor(StartValue, width, handleWidth);
        var end = PositionFor(EndValue, width, handleWidth);
        var low = Math.Min(start, end);
        var high = Math.Max(start, end);
        var radius = _tokens.TrackHeight / 2;
        var enabled = IsEffectivelyEnabled;
        var visuals = SliderDefaults.Visuals(_colors, enabled);
        var active = visuals.ActiveTrackColor;
        var inactive = visuals.InactiveTrackColor;
        var trackStart = handleWidth / 2;
        var trackEnd = width - handleWidth / 2;
        var startTrackEnd = low - handleWidth / 2 - _tokens.HandlePadding;
        var activeStart = low + handleWidth / 2 + _tokens.HandlePadding;
        var activeEnd = high - handleWidth / 2 - _tokens.HandlePadding;
        var endTrackStart = high + handleWidth / 2 + _tokens.HandlePadding;

        DrawTrack(context, inactive, trackStart, startTrackEnd, center, radius, TrackInsideCornerSize);
        DrawTrack(context, active, activeStart, activeEnd, center, TrackInsideCornerSize, TrackInsideCornerSize);
        DrawTrack(context, inactive, endTrackStart, trackEnd, center, TrackInsideCornerSize, radius);

        // RangeSlider draws a primary stop indicator at each outer inactive-track end.
        var stopRadius = _tokens.StopIndicatorSize / 2;
        if (startTrackEnd - trackStart > radius)
            context.DrawEllipse(active, null, new Point(trackStart + radius, center), stopRadius, stopRadius);
        if (trackEnd - endTrackStart > radius)
            context.DrawEllipse(active, null, new Point(trackEnd - radius, center), stopRadius, stopRadius);
        var brush = visuals.HandleColor;
        DrawHandle(context, start, center, handleWidth, brush);
        DrawHandle(context, end, center, handleWidth, brush);

        var activePosition = _startActive ? start : end;
        var stateLayerSize = _tokens.HandleHeight;
        var interactionArea = new Rect(
            activePosition - stateLayerSize / 2,
            center - stateLayerSize / 2,
            stateLayerSize,
            stateLayerSize);
        var focusRingArea = new Rect(
            activePosition - handleWidth / 2,
            center - _tokens.HandleHeight / 2,
            handleWidth,
            _tokens.HandleHeight).Inflate(4);
        _theme.InteractionPresentation.UpdateGeometry(
            new MaterialInteractionGeometry(
                interactionArea,
                focusRingArea,
                new CornerRadius(Math.Min(focusRingArea.Width, focusRingArea.Height) / 2)));
        _interaction.DrawLayers(context, visuals.StateLayerColor, new CornerRadius(stateLayerSize / 2));
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 240 : availableSize.Width, Math.Max(_tokens.HandleHeight, MinimumInteractiveComponentSize));

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEffectivelyEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        var width = Bounds.Width;
        var handle = _tokens.HandleWidth;
        var x = e.GetPosition(this).X;
        SetActiveEndpoint(
            Math.Abs(x - PositionFor(StartValue, width, handle)) <=
            Math.Abs(x - PositionFor(EndValue, width, handle)));
        e.Pointer.Capture(this);
        SetActiveFrom(x);
        Focus();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (Equals(e.Pointer.Captured, this))
            SetActiveFrom(e.GetPosition(this).X);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (Equals(e.Pointer.Captured, this))
            e.Pointer.Capture(null);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEffectivelyEnabled)
            return;

        if (e.Key == Key.Tab &&
            !e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            !e.KeyModifiers.HasFlag(KeyModifiers.Alt) &&
            !e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            var reverse = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if ((!reverse && _startActive) || (reverse && !_startActive))
            {
                SetActiveEndpoint(reverse);
                e.Handled = true;
            }

            return;
        }

        var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? LargeChange : SmallChange;
        var direction = FlowDirection == FlowDirection.RightToLeft ? -1 : 1;
        switch (e.Key)
        {
            case Key.Left:
                ChangeActive(-step * direction); e.Handled = true; break;
            case Key.Right:
                ChangeActive(step * direction); e.Handled = true; break;
            case Key.PageDown:
                ChangeActive(-LargeChange); e.Handled = true; break;
            case Key.PageUp:
                ChangeActive(LargeChange); e.Handled = true; break;
            case Key.Home:
                SetActive(Minimum); e.Handled = true; break;
            case Key.End:
                SetActive(Maximum); e.Handled = true; break;
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);

        if (e.NavigationMethod == NavigationMethod.Tab)
            SetActiveEndpoint(!e.KeyModifiers.HasFlag(KeyModifiers.Shift));
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialRangeSliderAutomationPeer(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEffectivelyEnabledProperty)
            ApplyTokens();
        if (!_updating)
        {
            if (change.Property == StartValueProperty)
                CoerceStartValue();
            else if (change.Property == EndValueProperty)
                CoerceEndValue();
            else if (change.Property == MinimumProperty || change.Property == MaximumProperty)
                NormalizeValues();
        }
    }

    private void NormalizeValues()
    {
        _updating = true;
        try
        {
            var maximum = Math.Max(Minimum, Maximum);
            if (maximum != Maximum) SetCurrentValue(MaximumProperty, maximum);
            var start = Math.Clamp(StartValue, Minimum, maximum);
            var end = Math.Clamp(EndValue, Minimum, maximum);
            if (start > end)
                start = end;
            if (start != StartValue) SetCurrentValue(StartValueProperty, start);
            if (end != EndValue) SetCurrentValue(EndValueProperty, end);
        }
        finally { _updating = false; }
    }

    private void CoerceStartValue()
    {
        _updating = true;
        try { SetCurrentValue(StartValueProperty, Math.Clamp(StartValue, Minimum, Math.Min(EndValue, Maximum))); }
        finally { _updating = false; }
    }

    private void CoerceEndValue()
    {
        _updating = true;
        try { SetCurrentValue(EndValueProperty, Math.Clamp(EndValue, Math.Max(StartValue, Minimum), Maximum)); }
        finally { _updating = false; }
    }

    private void SetActiveFrom(double x)
    {
        var travel = Bounds.Width - _tokens.HandleWidth;
        if (travel <= 0) return;
        var fraction = Math.Clamp((x - _tokens.HandleWidth / 2) / travel, 0, 1);
        SetActive(Minimum + (Maximum - Minimum) * fraction);
    }

    private void ChangeActive(double delta) => SetActive((_startActive ? StartValue : EndValue) + delta);

    private void SetActive(double value)
    {
        if (_startActive)
            StartValue = Math.Clamp(value, Minimum, EndValue);
        else
            EndValue = Math.Clamp(value, StartValue, Maximum);
    }

    private double PositionFor(double value, double width, double handleWidth)
    {
        var range = Maximum - Minimum;
        var fraction = range <= 0 ? 0 : Math.Clamp((value - Minimum) / range, 0, 1);
        return handleWidth / 2 + (width - handleWidth) * fraction;
    }

    internal bool IsStartEndpointActive => _startActive;

    internal Rect? EndpointFocusRingArea => _interaction.Geometry.FocusRingArea;

    internal bool IsEndpointFocusIndicatorActive =>
        _interaction.Interaction.HasFlag(InteractionState.Focused);

    internal Rect GetThumbBounds(bool startEndpoint)
    {
        var handleWidth = _tokens.HandleWidth;
        var center = Bounds.Height / 2;
        var position = PositionFor(startEndpoint ? StartValue : EndValue, Bounds.Width, handleWidth);
        return new Rect(
            position - handleWidth / 2,
            center - _tokens.HandleHeight / 2,
            handleWidth,
            _tokens.HandleHeight);
    }

    internal void FocusEndpoint(bool startEndpoint)
    {
        Focus(NavigationMethod.Tab);
        SetActiveEndpoint(startEndpoint);
    }

    private void SetActiveEndpoint(bool startEndpoint)
    {
        if (_startActive == startEndpoint)
            return;

        _startActive = startEndpoint;
        InvalidateVisual();
    }

    private void DrawTrack(
        DrawingContext context,
        IBrush brush,
        double from,
        double to,
        double center,
        double startRadius,
        double endRadius)
    {
        if (to - from < .5) return;
        context.DrawRectangle(
            brush,
            null,
            new RoundedRect(
                new Rect(from, center - _tokens.TrackHeight / 2, to - from, _tokens.TrackHeight),
                new CornerRadius(startRadius, endRadius, endRadius, startRadius)));
    }

    private void DrawHandle(DrawingContext context, double x, double center, double width, IBrush brush) =>
        context.DrawRectangle(brush, null, new RoundedRect(new Rect(x - width / 2, center - _tokens.HandleHeight / 2, width, _tokens.HandleHeight), new CornerRadius(width / 2)));

    private void ApplyTokens()
    {
        _tokens = SliderDefaults.Metrics();
        _colors = SliderDefaults.Colors(_theme.Theme, _theme.IsDark);
        InvalidateMeasure();
        InvalidateVisual();
    }
}
