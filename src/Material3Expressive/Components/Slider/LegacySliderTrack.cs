// Slider.kt / SliderDefaults.Track(SliderPositions), fixed AndroidX baseline.
// Copyright 2022 The Android Open Source Project. Licensed under Apache-2.0.
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>The continuous, round-capped legacy track, without thumb gaps or stop indicators.</summary>
/// <remarks>Reads the enclosing native Slider or RangeSlider. Set State to draw a standalone range.
/// The track only observes values; it never handles input or changes slider state.</remarks>
public sealed class LegacySliderTrack : Control
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<RangeSliderState?> StateProperty =
        AvaloniaProperty.Register<LegacySliderTrack, RangeSliderState?>(nameof(State));
    public RangeSliderState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public static readonly StyledProperty<SliderColors?> ColorsProperty = SliderTrack.ColorsProperty.AddOwner<LegacySliderTrack>();
    public SliderColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public static readonly StyledProperty<double> TrackHeightProperty = SliderTrack.TrackHeightProperty.AddOwner<LegacySliderTrack>();
    public double TrackHeight { get => GetValue(TrackHeightProperty); set => SetValue(TrackHeightProperty, value); }
    public static readonly StyledProperty<double> TickSizeProperty = SliderTrack.TickSizeProperty.AddOwner<LegacySliderTrack>();
    public double TickSize { get => GetValue(TickSizeProperty); set => SetValue(TickSizeProperty, value); }
    public static readonly StyledProperty<Color> ActiveTrackColorProperty = SliderTrack.ActiveTrackColorProperty.AddOwner<LegacySliderTrack>();
    public Color ActiveTrackColor { get => GetValue(ActiveTrackColorProperty); set => SetValue(ActiveTrackColorProperty, value); }
    public static readonly StyledProperty<Color> ActiveTickColorProperty = SliderTrack.ActiveTickColorProperty.AddOwner<LegacySliderTrack>();
    public Color ActiveTickColor { get => GetValue(ActiveTickColorProperty); set => SetValue(ActiveTickColorProperty, value); }
    public static readonly StyledProperty<Color> InactiveTrackColorProperty = SliderTrack.InactiveTrackColorProperty.AddOwner<LegacySliderTrack>();
    public Color InactiveTrackColor { get => GetValue(InactiveTrackColorProperty); set => SetValue(InactiveTrackColorProperty, value); }
    public static readonly StyledProperty<Color> InactiveTickColorProperty = SliderTrack.InactiveTickColorProperty.AddOwner<LegacySliderTrack>();
    public Color InactiveTickColor { get => GetValue(InactiveTickColorProperty); set => SetValue(InactiveTickColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledActiveTrackColorProperty = SliderTrack.DisabledActiveTrackColorProperty.AddOwner<LegacySliderTrack>();
    public Color DisabledActiveTrackColor { get => GetValue(DisabledActiveTrackColorProperty); set => SetValue(DisabledActiveTrackColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledActiveTickColorProperty = SliderTrack.DisabledActiveTickColorProperty.AddOwner<LegacySliderTrack>();
    public Color DisabledActiveTickColor { get => GetValue(DisabledActiveTickColorProperty); set => SetValue(DisabledActiveTickColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledInactiveTrackColorProperty = SliderTrack.DisabledInactiveTrackColorProperty.AddOwner<LegacySliderTrack>();
    public Color DisabledInactiveTrackColor { get => GetValue(DisabledInactiveTrackColorProperty); set => SetValue(DisabledInactiveTrackColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledInactiveTickColorProperty = SliderTrack.DisabledInactiveTickColorProperty.AddOwner<LegacySliderTrack>();
    public Color DisabledInactiveTickColor { get => GetValue(DisabledInactiveTickColorProperty); set => SetValue(DisabledInactiveTickColorProperty, value); }

    private Slider? _nativeOwner;
    private RangeSlider? _rangeOwner;
    private RangeSliderState? _observedState;
    private INotifyCollectionChanged? _ticks;
    private RangeSliderState? EffectiveState => State ?? _rangeOwner?.ActualState;
    internal bool Vertical => EffectiveState is null && _nativeOwner?.Orientation == Orientation.Vertical;
    internal float[] TickFractions => EffectiveState?.TickFractions ?? (_nativeOwner is { } owner ? SliderAppearance.TickFractions(owner) : []);
    internal SliderRange ActiveRange => new(EffectiveState?.StartFraction ?? 0,
        EffectiveState?.EndFraction ?? (_nativeOwner is { } owner ? SliderAppearance.Fraction(owner) : 1));

    static LegacySliderTrack()
    {
        AffectsMeasure<LegacySliderTrack>(TrackHeightProperty, StateProperty);
        AffectsRender<LegacySliderTrack>(StateProperty, ColorsProperty, TrackHeightProperty, TickSizeProperty,
            ActiveTrackColorProperty, ActiveTickColorProperty, InactiveTrackColorProperty, InactiveTickColorProperty,
            DisabledActiveTrackColorProperty, DisabledActiveTickColorProperty, DisabledInactiveTrackColorProperty,
            DisabledInactiveTickColorProperty, IsEffectivelyEnabledProperty, FlowDirectionProperty);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _nativeOwner = this.GetVisualAncestors().OfType<Slider>().FirstOrDefault();
        _rangeOwner = this.GetVisualAncestors().OfType<RangeSlider>().FirstOrDefault();
        if (_nativeOwner is { } native) native.PropertyChanged += OwnerChanged;
        if (_rangeOwner is { } range) range.PropertyChanged += OwnerChanged;
        ObserveState(); ObserveTicks(); InvalidateMeasure();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_nativeOwner is { } native) native.PropertyChanged -= OwnerChanged;
        if (_rangeOwner is { } range) range.PropertyChanged -= OwnerChanged;
        if (_observedState is { } state) state.PropertyChanged -= StateChanged;
        if (_ticks is { } ticks) ticks.CollectionChanged -= TicksChanged;
        _nativeOwner = null; _rangeOwner = null; _observedState = null; _ticks = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty && this.IsAttachedToVisualTree()) ObserveState();
    }

    private void ObserveState()
    {
        if (_observedState is { } old) old.PropertyChanged -= StateChanged;
        _observedState = EffectiveState;
        if (_observedState is { } state) state.PropertyChanged += StateChanged;
        InvalidateVisual();
    }

    private void ObserveTicks()
    {
        if (_ticks is { } old) old.CollectionChanged -= TicksChanged;
        _ticks = _nativeOwner?.Ticks;
        if (_ticks is { } ticks) ticks.CollectionChanged += TicksChanged;
    }

    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.TicksProperty) ObserveTicks();
        if (e.Property == RangeSlider.ActualStateProperty) ObserveState();
        if (e.Property == Slider.OrientationProperty) InvalidateMeasure();
        InvalidateVisual();
    }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => InvalidateVisual();
    private void TicksChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    protected override Size MeasureOverride(Size available) => Vertical
        ? new(Math.Min(TrackHeight, available.Width), double.IsFinite(available.Height) ? available.Height : 0)
        : new(double.IsFinite(available.Width) ? available.Width : 0, Math.Min(TrackHeight, available.Height));

    public override void Render(DrawingContext context)
    {
        var range = ActiveRange;
        var vertical = Vertical;
        var reverse = vertical ? _nativeOwner?.IsDirectionReversed != true
            : (FlowDirection == FlowDirection.RightToLeft) ^ (_nativeOwner?.IsDirectionReversed == true);
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var length = (float)(vertical ? Bounds.Height : Bounds.Width) * scale;
        var center = (float)(vertical ? Bounds.Width : Bounds.Height) * scale / 2;
        var start = reverse ? length : 0f;
        var end = reverse ? 0f : length;
        var strokeWidth = (float)TrackHeight * scale;
        var tickSize = (float)TickSize * scale;
        Point Offset(float axis) => vertical ? new(center / scale, axis / scale) : new(axis / scale, center / scale);
        var enabled = IsEffectivelyEnabled;
        var colors = Colors ?? _rangeOwner?.Colors ?? (_nativeOwner is { } native ? SliderAppearance.GetColors(native) : null);
        var inactive = enabled ? colors?.InactiveTrackColor ?? InactiveTrackColor : colors?.DisabledInactiveTrackColor ?? DisabledInactiveTrackColor;
        var active = enabled ? colors?.ActiveTrackColor ?? ActiveTrackColor : colors?.DisabledActiveTrackColor ?? DisabledActiveTrackColor;
        var inactiveTick = enabled ? colors?.InactiveTickColor ?? InactiveTickColor : colors?.DisabledInactiveTickColor ?? DisabledInactiveTickColor;
        var activeTick = enabled ? colors?.ActiveTickColor ?? ActiveTickColor : colors?.DisabledActiveTickColor ?? DisabledActiveTickColor;
        // Keep the two overlapping strokes. Replacing them by disjoint rounded rectangles
        // changes translucent colors, zero-length active ranges and the endpoint caps.
        context.DrawLine(new Pen(new SolidColorBrush(inactive), strokeWidth / scale, lineCap: PenLineCap.Round), Offset(start), Offset(end));
        context.DrawLine(new Pen(new SolidColorBrush(active), strokeWidth / scale, lineCap: PenLineCap.Round),
            Offset(start + (end - start) * range.Start), Offset(start + (end - start) * range.EndInclusive));
        foreach (var group in TickFractions.GroupBy(tick => tick > range.EndInclusive || tick < range.Start))
        {
            var brush = new SolidColorBrush(group.Key ? inactiveTick : activeTick);
            foreach (var tick in group)
                context.DrawEllipse(brush, null, Offset(SliderMath.Lerp(start, end, tick)), tickSize / 2 / scale, tickSize / 2 / scale);
        }
    }
}
