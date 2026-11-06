/*
 * Portions translated from AndroidX Slider.kt.
 * Copyright 2022 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

/// <summary>The ten state colors of Compose SliderColors.</summary>
public sealed record SliderColors(Color ThumbColor, Color ActiveTrackColor, Color ActiveTickColor, Color InactiveTrackColor, Color InactiveTickColor, Color DisabledThumbColor, Color DisabledActiveTrackColor, Color DisabledActiveTickColor, Color DisabledInactiveTrackColor, Color DisabledInactiveTickColor);

/// <summary>Default thumb content. Its layout size remains unchanged while its painted axis halves.</summary>
public sealed class SliderThumb : Control
{
    protected override bool BypassFlowDirectionPolicies => true;
    // Compose Modifier.size obeys the parent's maximum, whereas Avalonia Width/Height
    // may overflow their arrange slot. Keep the explicit thumb size inside its measured slot.
    protected override Size ArrangeOverride(Size finalSize) => finalSize.Constrain(DesiredSize);
    public static readonly StyledProperty<bool> IsVerticalProperty = AvaloniaProperty.Register<SliderThumb, bool>(nameof(IsVertical), false);
    public bool IsVertical { get => GetValue(IsVerticalProperty); set => SetValue(IsVerticalProperty, value); }
    public static readonly StyledProperty<InteractionState> InteractionProperty = AvaloniaProperty.Register<SliderThumb, InteractionState>(nameof(Interaction), InteractionState.None);
    public InteractionState Interaction { get => GetValue(InteractionProperty); set => SetValue(InteractionProperty, value); }
    public static readonly StyledProperty<Color> ThumbColorProperty = AvaloniaProperty.Register<SliderThumb, Color>(nameof(ThumbColor), default);
    public Color ThumbColor { get => GetValue(ThumbColorProperty); set => SetValue(ThumbColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledThumbColorProperty = AvaloniaProperty.Register<SliderThumb, Color>(nameof(DisabledThumbColor), default);
    public Color DisabledThumbColor { get => GetValue(DisabledThumbColorProperty); set => SetValue(DisabledThumbColorProperty, value); }
    public static readonly StyledProperty<Color> SurfaceColorProperty = AvaloniaProperty.Register<SliderThumb, Color>(nameof(SurfaceColor), default);
    public Color SurfaceColor { get => GetValue(SurfaceColorProperty); set => SetValue(SurfaceColorProperty, value); }
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = AvaloniaProperty.Register<SliderThumb, CornerRadius>(nameof(CornerRadius));
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public static readonly StyledProperty<SliderColors?> ColorsProperty = AvaloniaProperty.Register<SliderThumb, SliderColors?>(nameof(Colors), null);
    public SliderColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }

    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty =
        AvaloniaProperty.Register<SliderThumb, MaterialInteractionSource?>(nameof(InteractionSource));
    /// <summary>Observes shared feedback and emits hover events for this thumb's bounds.</summary>
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }
    private MaterialInteractionSource? _source;
    private Action<MaterialInteractionEvent>? _sourceListener;
    private MaterialInteractionEvent.HoverEnter? _hover;
    private readonly List<MaterialInteractionEvent> _events = [];
    private TopLevel? _hoverRoot;
    private Avalonia.Input.PointerEventArgs? _lastPointer;
    private bool _hoverReset;
    private bool _hoverAwaitExit;

    public SliderThumb()
    {
        LayoutUpdated += (_, _) => { if (_lastPointer is { } pointer) UpdateCapturedHover(pointer); };
    }

    private void ConnectSource(bool clearEvents = true)
    {
        if (!clearEvents && _hover is not null) _hoverAwaitExit = true;
        DisconnectSource(clearEvents);
        if (InteractionSource is { } source)
        {
            _source = source;
            source.Interaction += _sourceListener = interaction => { if (_source == source) ReceiveInteraction(interaction); };
        }
    }
    private void DisconnectSource(bool clearEvents = true)
    {
        EndHover();
        if (_source is not null) _source.Interaction -= _sourceListener;
        if (clearEvents && _source is not null) SetCurrentValue(InteractionProperty, InteractionState.None);
        _source = null;
        if (clearEvents) { _events.Clear(); _hoverAwaitExit = false; }
    }
    private void ReceiveInteraction(MaterialInteractionEvent interaction) =>
        SetCurrentValue(InteractionProperty, SliderInteraction.Update(_events, interaction));
    internal void EndHover(bool waitForExit = false)
    {
        if (waitForExit && _hover is not null) _hoverAwaitExit = true;
        _lastPointer = null;
        _hoverReset = true;
        var hover = _hover; _hover = null;
        if (hover is not null) _source?.Emit(new MaterialInteractionEvent.HoverExit(hover));
    }
    protected override void OnPointerEntered(Avalonia.Input.PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!_hoverReset && !_hoverAwaitExit && _source is not null && _hover is null)
        {
            var hover = _hover = new();
            _source.Emit(hover);
        }
    }
    protected override void OnPointerExited(Avalonia.Input.PointerEventArgs e)
    {
        base.OnPointerExited(e);
        // RangeSlider captures its layout node, not the thumb. Native pointer-over then excludes
        // the thumb even while the pointer remains inside the hoverable's unchanged layout box.
        if (e.Pointer.Captured is Visual captured && captured.IsVisualAncestorOf(this)
            && new Rect(Bounds.Size).Contains(e.GetPosition(this))) return;
        _hoverAwaitExit = false;
        EndHover();
    }
    private void ObservePointer(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        _hoverReset = false;
        _lastPointer = e;
        UpdateCapturedHover(e);
    }
    private void UpdateCapturedHover(Avalonia.Input.PointerEventArgs e)
    {
        if (!this.IsAttachedToVisualTree()) return;
        var inside = new Rect(Bounds.Size).Contains(e.GetPosition(this));
        if (inside && e.Pointer.Captured is not Visual)
        {
            // A top-level observer must still respect clipping and occlusion. Disabled thumbs
            // are hoverable upstream, so use visual hit testing rather than enabled input hits.
            var hit = _hoverRoot?.GetVisualAt(e.GetPosition(_hoverRoot));
            inside = hit == this || hit is not null && this.IsVisualAncestorOf(hit);
        }
        else if (inside && e.Pointer.Captured is Visual capture && !capture.IsVisualAncestorOf(this))
            inside = capture == this;
        if (!inside)
        {
            _hoverAwaitExit = false;
            var hover = _hover; _hover = null;
            if (hover is not null) _source?.Emit(new MaterialInteractionEvent.HoverExit(hover));
        }
        else if (!_hoverAwaitExit && _hover is null && _source is not null)
        {
            var hover = _hover = new();
            _source.Emit(hover);
        }
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InteractionSourceProperty && this.IsAttachedToVisualTree())
        {
            // ThumbContent remembers its list outside LaunchedEffect(interactionSource).
            ConnectSource(clearEvents: InteractionSource is null);
            RefreshNativeFeedback();
        }
    }

    private Avalonia.Controls.Slider? _nativeOwner;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ConnectSource();
        _hoverRoot = TopLevel.GetTopLevel(this);
        _hoverRoot?.AddHandler(PointerMovedEvent, ObservePointer, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
        _hoverRoot?.AddHandler(PointerReleasedEvent, ObservePointer, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
        _nativeOwner = this.GetVisualAncestors().OfType<Avalonia.Controls.Slider>().FirstOrDefault();
        if (_nativeOwner is { } owner)
        {
            owner.PropertyChanged += NativeChanged;
            owner.Classes.CollectionChanged += NativeClassesChanged;
            RefreshNativeFeedback();
        }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_nativeOwner is { } owner)
        {
            owner.PropertyChanged -= NativeChanged;
            owner.Classes.CollectionChanged -= NativeClassesChanged;
        }
        _nativeOwner = null;
        _hoverRoot?.RemoveHandler(PointerMovedEvent, ObservePointer);
        _hoverRoot?.RemoveHandler(PointerReleasedEvent, ObservePointer);
        _hoverRoot = null;
        DisconnectSource();
        base.OnDetachedFromVisualTree(e);
    }
    private void NativeChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => RefreshNativeFeedback();
    private void NativeClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshNativeFeedback();
    private void RefreshNativeFeedback()
    {
        if (_nativeOwner is not { } owner) return;
        SetCurrentValue(IsVerticalProperty, owner.Orientation == Orientation.Vertical);
        if (_source is not null) return;
        SetCurrentValue(InteractionProperty, !owner.IsEffectivelyEnabled ? InteractionState.None
            : (owner.IsFocused ? InteractionState.Focused : InteractionState.None)
                | (owner.Classes.Contains(":pressed") ? InteractionState.Pressed : InteractionState.None));
    }
    static SliderThumb() => AffectsRender<SliderThumb>(IsVerticalProperty, InteractionProperty, ThumbColorProperty, DisabledThumbColorProperty, SurfaceColorProperty, ColorsProperty, CornerRadiusProperty, IsEffectivelyEnabledProperty);
    public override void Render(DrawingContext context)
    {
        var active = (Interaction & (InteractionState.Pressed | InteractionState.Dragged | InteractionState.Focused)) != 0;
        var size = new Size(Bounds.Width / (!IsVertical && active ? 2 : 1), Bounds.Height / (IsVertical && active ? 2 : 1));
        var rect = new Rect((Bounds.Width - size.Width) / 2, (Bounds.Height - size.Height) / 2, size.Width, size.Height);
        // SliderDefaults precomposes its default disabled handle over Surface;
        // an explicitly supplied SliderColors already contains the caller's final color.
        var color = IsEffectivelyEnabled ? Colors?.ThumbColor ?? ThumbColor
            : Colors?.DisabledThumbColor ?? ColorComposite.Over(DisabledThumbColor, 1, SurfaceColor);
        var full = Math.Min(size.Width, size.Height) / 2;
        var radius = new CornerRadius(Math.Min(CornerRadius.TopLeft, full), Math.Min(CornerRadius.TopRight, full), Math.Min(CornerRadius.BottomRight, full), Math.Min(CornerRadius.BottomLeft, full));
        context.DrawRectangle(new SolidColorBrush(color), null, new RoundedRect(rect, radius));
    }
}

/// <summary>Material track drawing. Range controls supply State; native Slider templates consume their owner.</summary>
public sealed class SliderTrack : Control
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<AvaloniaObject?> StateProperty = AvaloniaProperty.Register<SliderTrack, AvaloniaObject?>(nameof(State), null);
    public AvaloniaObject? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public static readonly StyledProperty<SliderColors?> ColorsProperty = AvaloniaProperty.Register<SliderTrack, SliderColors?>(nameof(Colors), null);
    public SliderColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public static readonly StyledProperty<double> TrackHeightProperty = AvaloniaProperty.Register<SliderTrack, double>(nameof(TrackHeight));
    public double TrackHeight { get => GetValue(TrackHeightProperty); set => SetValue(TrackHeightProperty, value); }
    public static readonly StyledProperty<double> ThumbTrackGapSizeProperty = AvaloniaProperty.Register<SliderTrack, double>(nameof(ThumbTrackGapSize));
    public double ThumbTrackGapSize { get => GetValue(ThumbTrackGapSizeProperty); set => SetValue(ThumbTrackGapSizeProperty, value); }
    public static readonly StyledProperty<double> TrackInsideCornerSizeProperty = AvaloniaProperty.Register<SliderTrack, double>(nameof(TrackInsideCornerSize));
    public double TrackInsideCornerSize { get => GetValue(TrackInsideCornerSizeProperty); set => SetValue(TrackInsideCornerSizeProperty, value); }
    public static readonly StyledProperty<double> TrackCornerSizeProperty = AvaloniaProperty.Register<SliderTrack, double>(nameof(TrackCornerSize), double.NaN);
    public double TrackCornerSize { get => GetValue(TrackCornerSizeProperty); set => SetValue(TrackCornerSizeProperty, value); }
    public static readonly StyledProperty<bool> IsCenteredProperty = AvaloniaProperty.Register<SliderTrack, bool>(nameof(IsCentered), false);
    public bool IsCentered { get => GetValue(IsCenteredProperty); set => SetValue(IsCenteredProperty, value); }
    public static readonly StyledProperty<bool> EnableCornerShrinkingProperty = AvaloniaProperty.Register<SliderTrack, bool>(nameof(EnableCornerShrinking), false);
    public bool EnableCornerShrinking { get => GetValue(EnableCornerShrinkingProperty); set => SetValue(EnableCornerShrinkingProperty, value); }
    public static readonly StyledProperty<bool> ShowStopIndicatorProperty = AvaloniaProperty.Register<SliderTrack, bool>(nameof(ShowStopIndicator), true);
    public bool ShowStopIndicator { get => GetValue(ShowStopIndicatorProperty); set => SetValue(ShowStopIndicatorProperty, value); }
    public static readonly StyledProperty<double> TickSizeProperty = AvaloniaProperty.Register<SliderTrack, double>(nameof(TickSize));
    public double TickSize { get => GetValue(TickSizeProperty); set => SetValue(TickSizeProperty, value); }
    public static readonly StyledProperty<double> StopIndicatorSizeProperty = AvaloniaProperty.Register<SliderTrack, double>(nameof(StopIndicatorSize));
    public double StopIndicatorSize { get => GetValue(StopIndicatorSizeProperty); set => SetValue(StopIndicatorSizeProperty, value); }

    public static readonly StyledProperty<Color> ActiveTrackColorProperty = AvaloniaProperty.Register<SliderTrack, Color>(nameof(ActiveTrackColor), default);
    public Color ActiveTrackColor { get => GetValue(ActiveTrackColorProperty); set => SetValue(ActiveTrackColorProperty, value); }
    public static readonly StyledProperty<Color> ActiveTickColorProperty = AvaloniaProperty.Register<SliderTrack, Color>(nameof(ActiveTickColor), default);
    public Color ActiveTickColor { get => GetValue(ActiveTickColorProperty); set => SetValue(ActiveTickColorProperty, value); }
    public static readonly StyledProperty<Color> InactiveTrackColorProperty = AvaloniaProperty.Register<SliderTrack, Color>(nameof(InactiveTrackColor), default);
    public Color InactiveTrackColor { get => GetValue(InactiveTrackColorProperty); set => SetValue(InactiveTrackColorProperty, value); }
    public static readonly StyledProperty<Color> InactiveTickColorProperty = AvaloniaProperty.Register<SliderTrack, Color>(nameof(InactiveTickColor), default);
    public Color InactiveTickColor { get => GetValue(InactiveTickColorProperty); set => SetValue(InactiveTickColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledActiveTrackColorProperty = AvaloniaProperty.Register<SliderTrack, Color>(nameof(DisabledActiveTrackColor), default);
    public Color DisabledActiveTrackColor { get => GetValue(DisabledActiveTrackColorProperty); set => SetValue(DisabledActiveTrackColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledActiveTickColorProperty = AvaloniaProperty.Register<SliderTrack, Color>(nameof(DisabledActiveTickColor), default);
    public Color DisabledActiveTickColor { get => GetValue(DisabledActiveTickColorProperty); set => SetValue(DisabledActiveTickColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledInactiveTrackColorProperty = AvaloniaProperty.Register<SliderTrack, Color>(nameof(DisabledInactiveTrackColor), default);
    public Color DisabledInactiveTrackColor { get => GetValue(DisabledInactiveTrackColorProperty); set => SetValue(DisabledInactiveTrackColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledInactiveTickColorProperty = AvaloniaProperty.Register<SliderTrack, Color>(nameof(DisabledInactiveTickColor), default);
    public Color DisabledInactiveTickColor { get => GetValue(DisabledInactiveTickColorProperty); set => SetValue(DisabledInactiveTickColorProperty, value); }

    /// <summary>Override drawing in track-local DIP. Null uses the default circular tick.</summary>
    private Action<DrawingContext, Point, Color>? _drawTick;
    public Action<DrawingContext, Point, Color>? DrawTick { get => _drawTick; set { _drawTick = value; InvalidateVisual(); } }
    /// <summary>Override drawing in track-local DIP. ShowStopIndicator=false omits it and restores endpoint ticks.</summary>
    private Action<DrawingContext, Point, Color>? _drawStopIndicator;
    public Action<DrawingContext, Point, Color>? DrawStopIndicator { get => _drawStopIndicator; set { _drawStopIndicator = value; InvalidateVisual(); } }
    private Avalonia.Controls.Slider? _nativeOwner;
    private RangeSlider? _rangeOwner;
    private INotifyCollectionChanged? _ticks;
    internal bool Vertical => _nativeOwner?.Orientation == Orientation.Vertical;
    internal float[] TickFractions => State is RangeSliderState range ? range.TickFractions
        : _nativeOwner is { } owner ? SliderAppearance.TickFractions(owner) : [];

    static SliderTrack()
    {
        AffectsMeasure<SliderTrack>(StateProperty, TrackHeightProperty);
        AffectsRender<SliderTrack>(StateProperty, ColorsProperty, TrackHeightProperty, ThumbTrackGapSizeProperty, TrackInsideCornerSizeProperty, TrackCornerSizeProperty,
            IsCenteredProperty, EnableCornerShrinkingProperty, ShowStopIndicatorProperty, TickSizeProperty, StopIndicatorSizeProperty,
            ActiveTrackColorProperty, ActiveTickColorProperty, InactiveTrackColorProperty, InactiveTickColorProperty, DisabledActiveTrackColorProperty, DisabledActiveTickColorProperty, DisabledInactiveTrackColorProperty, DisabledInactiveTickColorProperty, IsEffectivelyEnabledProperty, FlowDirectionProperty);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // This alignment line changes discrete thumb placement even when track size stays the same.
        if (change.Property == TrackCornerSizeProperty)
        {
            this.GetVisualAncestors().OfType<SliderLayout>().FirstOrDefault()?.InvalidateArrange();
            this.GetVisualParent()?.GetVisualChildren().OfType<MaterialSliderTrack>().FirstOrDefault()?.InvalidateArrange();
        }
        if (change.Property == StateProperty)
        {
            if (change.OldValue is AvaloniaObject old) old.PropertyChanged -= StateChanged;
            if (change.NewValue is AvaloniaObject state)
            {
                if (state is not RangeSliderState) throw new ArgumentException("SliderTrack.State requires RangeSliderState.");
                if (this.IsAttachedToVisualTree()) state.PropertyChanged += StateChanged;
            }
        }
    }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => InvalidateVisual();
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (State is { } state) { state.PropertyChanged -= StateChanged; state.PropertyChanged += StateChanged; }
        _nativeOwner = this.GetVisualAncestors().OfType<Avalonia.Controls.Slider>().FirstOrDefault();
        if (_nativeOwner is { } owner) { owner.PropertyChanged += NativeChanged; SubscribeTicks(); InvalidateMeasure(); }
        _rangeOwner = this.GetVisualAncestors().OfType<RangeSlider>().FirstOrDefault();
        if (_rangeOwner is { } range) range.PropertyChanged += NativeChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (State is { } state) state.PropertyChanged -= StateChanged;
        if (_nativeOwner is { } owner) owner.PropertyChanged -= NativeChanged;
        if (_rangeOwner is { } range) range.PropertyChanged -= NativeChanged;
        _rangeOwner = null;
        if (_ticks is not null) _ticks.CollectionChanged -= TicksChanged;
        _ticks = null; _nativeOwner = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void NativeChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Avalonia.Controls.Slider.TicksProperty) SubscribeTicks();
        if (e.Property == Avalonia.Controls.Slider.OrientationProperty) InvalidateMeasure();
        if (e.Property == Avalonia.Controls.Slider.TicksProperty || e.Property == Avalonia.Controls.Slider.TickFrequencyProperty)
            this.GetVisualParent()?.GetVisualChildren().OfType<MaterialSliderTrack>().FirstOrDefault()?.InvalidateArrange();
        InvalidateVisual();
    }
    private void SubscribeTicks()
    {
        if (_ticks is not null) _ticks.CollectionChanged -= TicksChanged;
        _ticks = _nativeOwner?.Ticks;
        if (_ticks is not null) _ticks.CollectionChanged += TicksChanged;
    }
    private void TicksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
        this.GetVisualParent()?.GetVisualChildren().OfType<MaterialSliderTrack>().FirstOrDefault()?.InvalidateArrange();
    }
    protected override Size MeasureOverride(Size available) => Vertical
        ? new(Math.Min(TrackHeight, available.Width), double.IsFinite(available.Height) ? available.Height : 0)
        : new(double.IsFinite(available.Width) ? available.Width : 0, Math.Min(TrackHeight, available.Height));

    public override void Render(DrawingContext context)
    {
        if (State is not RangeSliderState && _nativeOwner is null) return;
        var range = State as RangeSliderState; var owner = _nativeOwner;
        var vertical = Vertical;
        var rtl = (FlowDirection == FlowDirection.RightToLeft) ^ (!vertical && owner?.IsDirectionReversed == true);
        var rtlHorizontal = rtl && !vertical;
        var centered = range is null && IsCentered;
        var ticks = TickFractions;
        var fractionStart = range?.StartFraction ?? 0;
        var fractionEnd = range?.EndFraction ?? SliderAppearance.Fraction(owner!);
        var scale = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var width = (float)Bounds.Width * scale; var height = (float)Bounds.Height * scale;
        var corner = double.IsNaN(TrackCornerSize) ? (vertical ? width : height) / 2 : (float)TrackCornerSize * scale;
        var inside = (float)TrackInsideCornerSize * scale;
        var length = vertical ? height : width;
        bool Endpoint(float fraction) => fraction == 0 || fraction == 1;
        float Position(float fraction) => ticks.Length > 0 && !Endpoint(fraction) ? (length - corner * 2) * fraction + corner : length * fraction;
        var valueStart = Position(fractionStart); var valueEnd = Position(fractionEnd); var center = length / 2;
        var nativeThumb = owner?.GetVisualDescendants().OfType<MaterialSliderTrack>().FirstOrDefault()?.Thumb;
        var nativeExtent = (float)(vertical ? nativeThumb?.Bounds.Height ?? 0 : nativeThumb?.Bounds.Width ?? 0) * scale;
        var startThumb = range is not null ? range.StartThumbWidth : centered && fractionEnd <= 0.5f ? nativeExtent : 0;
        var endThumb = range is not null ? range.EndThumbWidth : !centered || fractionEnd >= 0.5f ? nativeExtent : 0;
        var startSpacing = (centered || range is not null) ? (float)ThumbTrackGapSize * scale : 0;
        var endSpacing = (float)ThumbTrackGapSize * scale;
        var rangeOwner = _rangeOwner;
        var focusOwner = (Control?)rangeOwner ?? owner;
        if (focusOwner is not null && MaterialInteraction.GetFocusFeedback(focusOwner) == FocusFeedback.InsetRing)
        {
            var startFocused = rangeOwner?.IsThumbFocused(true) ?? owner?.GetValue(SliderAppearance.IsSourceFocusedProperty) == true;
            var endFocused = rangeOwner?.IsThumbFocused(false) ?? owner?.GetValue(SliderAppearance.IsSourceFocusedProperty) == true;
            if ((centered || range is not null) && startFocused) startSpacing += 4 * scale;
            if (endFocused) endSpacing += 4 * scale;
        }
        var startGap = startSpacing > 0 ? startThumb / 2 + startSpacing : 0;
        var endGap = endSpacing > 0 ? endThumb / 2 + endSpacing : 0;
        var shrinking = range is null && EnableCornerShrinking;
        var enabled = IsEffectivelyEnabled;
        var active = enabled ? Colors?.ActiveTrackColor ?? ActiveTrackColor : Colors?.DisabledActiveTrackColor ?? DisabledActiveTrackColor;
        var inactive = enabled ? Colors?.InactiveTrackColor ?? InactiveTrackColor : Colors?.DisabledInactiveTrackColor ?? DisabledInactiveTrackColor;
        var activeTick = enabled ? Colors?.ActiveTickColor ?? ActiveTickColor : Colors?.DisabledActiveTickColor ?? DisabledActiveTickColor;
        var inactiveTick = enabled ? Colors?.InactiveTickColor ?? InactiveTickColor : Colors?.DisabledInactiveTickColor ?? DisabledInactiveTickColor;
        using var transform = vertical && owner?.IsDirectionReversed == false
            ? context.PushTransform(new Matrix(1, 0, 0, -1, 0, Bounds.Height)) : default;
        void Path(float from, float to, Color color, float startRadius, float endRadius)
        {
            var start = rtlHorizontal ? length - to : from;
            var rect = vertical ? new Rect(0, start / scale, width / scale, (to - from) / scale)
                : new Rect(start / scale, 0, (to - from) / scale, height / scale);
            var radii = vertical ? new CornerRadius(startRadius / scale, startRadius / scale, endRadius / scale, endRadius / scale)
                : new CornerRadius(startRadius / scale, endRadius / scale, endRadius / scale, startRadius / scale);
            context.DrawRectangle(new SolidColorBrush(color), null, new RoundedRect(rect, radii));
        }
        Point Offset(float axis) => vertical ? new(width / scale / 2, axis / scale)
            : new((rtl ? width - axis : axis) / scale, height / scale / 2);
        void Stop(float axis)
        {
            if (!ShowStopIndicator) return;
            var position = Offset(axis);
            if (DrawStopIndicator is { } draw) draw(context, position, active);
            else context.DrawEllipse(new SolidColorBrush(active), null, position, StopIndicatorSize / 2, StopIndicatorSize / 2);
        }
        var adjustedEnd = centered ? MathF.Min(valueEnd, center) : valueStart;
        var startThreshold = startGap + (!shrinking || ticks.Length > 0 ? corner : 0);
        if ((centered || range is not null) && adjustedEnd > startThreshold)
        {
            Path(0, adjustedEnd - startGap, inactive, rtlHorizontal ? inside : corner, rtlHorizontal ? corner : inside);
            Stop(corner);
        }
        var adjustedStart = centered ? MathF.Max(valueEnd, center) : valueEnd;
        var endThreshold = length - endGap - (!shrinking || ticks.Length > 0 ? corner : 0);
        if (adjustedStart < endThreshold)
        {
            Path(adjustedStart + endGap, length, inactive, rtlHorizontal ? corner : inside, rtlHorizontal ? inside : corner);
            Stop(length - corner);
        }
        var activeStart = centered ? adjustedEnd + (adjustedEnd < center ? startGap : 0) : range is not null ? valueStart + startGap : 0;
        var activeEnd = centered ? adjustedStart - (adjustedStart > center ? endGap : 0) : valueEnd - endGap;
        var startRadius = rtlHorizontal || centered || range is not null ? inside : corner;
        var endRadius = rtlHorizontal && !centered && range is null ? corner : inside;
        var activeLength = rtlHorizontal && !centered && range is null ? activeEnd : activeEnd - activeStart;
        if (activeLength > (!shrinking || ticks.Length > 0 ? startRadius : 0)) Path(activeStart, activeEnd, active, startRadius, endRadius);
        bool In(float value, float start, float end) => value >= start && value <= end;
        for (var i = 0; i < ticks.Length; i++)
        {
            if (ShowStopIndicator && (((centered || range is not null) && ticks[i] == 0) || ticks[i] == 1)) continue;
            var position = SliderMath.Lerp(corner, length - corner, ticks[i]);
            var centerGap = valueEnd > center ? startGap : endGap;
            var thumbGap = centered ? valueEnd > center ? endGap : startGap : endGap;
            if (centered && In(position, center - centerGap, center + centerGap)
                || range is not null && In(position, valueStart - startGap, valueStart + startGap)
                || In(position, valueEnd - thumbGap, valueEnd + thumbGap)) continue;
            var color = In(position, activeStart, activeEnd) ? activeTick : inactiveTick;
            if (DrawTick is { } draw) draw(context, Offset(position), color);
            else context.DrawEllipse(new SolidColorBrush(color), null, Offset(position), TickSize / 2, TickSize / 2);
        }
    }
}

// The upstream padded indication is placed outside the unchanged core thumb layout.
// Keeping the core slot unchanged also preserves native Track's input/value density.
internal sealed class SliderThumbPresenter : ContentPresenter
{
    internal RangeSlider? Owner;
    internal bool IsStart;
    private readonly InsetFocusRingPresenter _ring = new() { ZIndex = 1 };
    private Slider? _nativeOwner;
    private readonly MaterialInteractionSource _nativeSource = new();
    private MaterialInteractionSource? _observedNativeSource;
    private MaterialInteractionSource NativeSource => _observedNativeSource ?? _nativeSource;
    private readonly List<MaterialInteractionEvent.Focus> _nativeFocusEvents = [];
    private MaterialInteractionEvent.Press? _nativePress;
    private Point _nativePressPosition;
    private bool _nativeReleasing;
    private MaterialInteractionEvent.Focus? _nativeFocus;
    private Thumb? _nativeThumb;
    private MaterialInteractionEvent.HoverEnter? _nativeHover;
    private MaterialInteractionEvent.DragStart? _nativeDrag;
    internal void ObserveThumb(Thumb? thumb)
    {
        if (ReferenceEquals(thumb, _nativeThumb)) return;
        if (_nativeThumb is { } previous)
        {
            previous.PropertyChanged -= ThumbChanged;
            previous.DragStarted -= ThumbDragStarted;
            previous.DragCompleted -= ThumbDragCompleted;
            previous.RemoveHandler(PointerCaptureLostEvent, NativeCaptureLost);
        }
        EndNativeHover(); EndNativeDrag(cancel: true);
        _nativeThumb = thumb;
        if (thumb is not null)
        {
            thumb.PropertyChanged += ThumbChanged;
            thumb.DragStarted += ThumbDragStarted;
            thumb.DragCompleted += ThumbDragCompleted;
            thumb.AddHandler(PointerCaptureLostEvent, NativeCaptureLost, Avalonia.Interactivity.RoutingStrategies.Direct);
        }
    }
    private void ThumbChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != IsPointerOverProperty) return;
        // A default SliderThumb publishes its own hoverable bounds when explicitly
        // connected. Custom native thumbs still need this template bridge.
        if (_nativeThumb?.GetVisualDescendants().OfType<SliderThumb>().Any(t => t.InteractionSource == _observedNativeSource) == true) return;
        if (_nativeThumb?.IsPointerOver == true && _nativeHover is null)
        {
            _nativeHover = new(); NativeSource.Emit(_nativeHover);
        }
        else if (_nativeThumb?.IsPointerOver != true) EndNativeHover();
    }
    private void EndNativeHover()
    {
        var hover = _nativeHover; _nativeHover = null;
        if (hover is not null) NativeSource.Emit(new MaterialInteractionEvent.HoverExit(hover));
    }
    private void ThumbDragStarted(object? sender, VectorEventArgs e)
    {
        if (_nativeDrag is not null) return;
        EndNativePress(cancel: true);
        _nativeDrag = new(); NativeSource.Emit(_nativeDrag);
    }
    private void ThumbDragCompleted(object? sender, VectorEventArgs e) => EndNativeDrag();
    private void EndNativeDrag(bool cancel = false)
    {
        var drag = _nativeDrag; _nativeDrag = null;
        if (drag is not null) NativeSource.Emit(cancel ? new MaterialInteractionEvent.DragCancel(drag) : new MaterialInteractionEvent.DragStop(drag));
    }
    internal double FocusPadding => MaterialInteraction.GetFocusFeedback((Control?)Owner ?? (Control?)_nativeOwner ?? this) == FocusFeedback.InsetRing
        && (Owner is { } owner ? owner.IsThumbFocused(IsStart) : _nativeOwner?.GetValue(SliderAppearance.IsSourceFocusedProperty) == true) ? 4 : 0;
    internal InsetFocusRingPresenter Ring => _ring;

    public SliderThumbPresenter() => VisualChildren.Add(_ring);
    internal void ConnectRing()
    {
        if (!this.IsAttachedToVisualTree()) return;
        if (Owner is { } owner) _ring.Connect(owner, owner.ThumbSource(IsStart));
        else if (_nativeOwner is { } native) _ring.Connect(native, NativeSource);
        RefreshRing();
    }
    internal void RefreshRing()
    {
        _ring.Corners = CornerRadius;
        InvalidateArrange();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _nativeOwner = this.GetVisualAncestors().OfType<Slider>().FirstOrDefault();
        if (_nativeOwner is { } owner)
        {
            // The native Track adds this indication host as a visual-only sibling.
            ((ISetInheritanceParent)this).SetParent(owner);
            ApplyStyling();
            owner.PropertyChanged += NativeChanged;
            owner.GotFocus += NativeGotFocus;
            owner.LostFocus += NativeLostFocus;
            MaterialInputMode.For(owner);
            owner.Classes.CollectionChanged += NativeClassesChanged;
            owner.AddHandler(PointerPressedEvent, NativePointerDown, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
            owner.AddHandler(PointerReleasedEvent, NativePointerUp, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
            ConnectNativeSource();
        }
        ConnectRing();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_nativeOwner is { } owner)
        {
            owner.PropertyChanged -= NativeChanged;
            owner.GotFocus -= NativeGotFocus;
            owner.LostFocus -= NativeLostFocus;
            owner.Classes.CollectionChanged -= NativeClassesChanged;
            owner.RemoveHandler(PointerPressedEvent, NativePointerDown);
            owner.RemoveHandler(PointerReleasedEvent, NativePointerUp);
            ((ISetInheritanceParent)this).SetParent(null);
        }
        EndNativeFocus(); EndNativePress(cancel: true);
        ObserveThumb(null);
        if (_observedNativeSource is { } source) source.Interaction -= NativeInteraction;
        _observedNativeSource = null; _nativeFocusEvents.Clear();
        _nativeOwner?.ClearValue(SliderAppearance.IsSourceFocusedProperty);
        _nativeOwner = null;
        _ring.Disconnect();
        base.OnDetachedFromVisualTree(e);
    }
    private void NativeGotFocus(object? sender, Avalonia.Input.FocusChangedEventArgs e)
    {
        if (_nativeOwner is not { IsFocused: true, IsEffectivelyEnabled: true } owner || _nativeFocus is not null) return;
        MaterialInputMode.For(owner)?.ObserveFocusNavigation(e.NavigationMethod);
        _nativeFocus = new(); NativeSource.Emit(_nativeFocus);
    }
    private void NativeLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => EndNativeFocus();
    private void EndNativeFocus()
    {
        var focus = _nativeFocus; _nativeFocus = null;
        if (focus is not null) NativeSource.Emit(new MaterialInteractionEvent.Unfocus(focus));
    }
    private void NativeChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsEffectivelyEnabledProperty && _nativeOwner?.IsEffectivelyEnabled == false)
        { EndNativeFocus(); EndNativePress(cancel: true); EndNativeDrag(cancel: true); }
        if (e.Property == SliderAppearance.InteractionSourceProperty) ConnectNativeSource();
        if (e.Property == IsFocusedProperty || e.Property == MaterialInteraction.FocusFeedbackProperty || e.Property == SliderAppearance.IsSourceFocusedProperty) RefreshRing();
    }
    private void ConnectNativeSource()
    {
        if (_nativeOwner is not { } owner) return;
        var next = SliderAppearance.GetInteractionSource(owner) ?? _nativeSource;
        if (ReferenceEquals(next, _observedNativeSource)) return;
        EndNativePress(cancel: true); EndNativeDrag(cancel: true); EndNativeFocus(); EndNativeHover();
        if (_observedNativeSource is { } old) old.Interaction -= NativeInteraction;
        _nativeFocusEvents.Clear();
        _observedNativeSource = next;
        next.Interaction += NativeInteraction;
        // collectIsFocusedAsState retains its last value when the effect's source key
        // changes. The new source's next event recomputes it from its empty focus list.
        ConnectRing();
    }
    private void NativeInteraction(MaterialInteractionEvent interaction)
    {
        if (interaction is MaterialInteractionEvent.Focus focus) _nativeFocusEvents.Add(focus);
        else if (interaction is MaterialInteractionEvent.Unfocus unfocus) _nativeFocusEvents.Remove(unfocus.Start);
        _nativeOwner?.SetValue(SliderAppearance.IsSourceFocusedProperty, _nativeFocusEvents.Count > 0);
    }
    private void NativePointerDown(object? sender, PointerPressedEventArgs e)
    {
        _nativeReleasing = false;
        _nativePressPosition = e.GetPosition(_nativeThumb ?? (Visual?)_nativeOwner);
    }
    private void NativePointerUp(object? sender, PointerReleasedEventArgs e)
    {
        _nativeReleasing = true;
        EndNativePress(cancel: false);
    }
    private void NativeClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_nativeOwner is { IsEffectivelyEnabled: true } owner && owner.Classes.Contains(":pressed"))
        {
            if (_nativePress is null && _nativeDrag is null)
            {
                _nativePress = new(_nativePressPosition); NativeSource.Emit(_nativePress);
            }
        }
        else EndNativePress(cancel: !_nativeReleasing);
    }
    private void EndNativePress(bool cancel)
    {
        var press = _nativePress; _nativePress = null;
        if (press is not null) NativeSource.Emit(cancel ? new MaterialInteractionEvent.CancelPress(press) : new MaterialInteractionEvent.Release(press));
    }
    private void NativeCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        EndNativePress(cancel: !_nativeReleasing);
        EndNativeDrag(cancel: !_nativeReleasing);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CornerRadiusProperty) RefreshRing();
        // ContentPresenter inserts replacement content after existing visual-only children.
        if (change.Property == ChildProperty)
        {
            VisualChildren.Remove(_ring);
            VisualChildren.Add(_ring);
            ConnectRing();
        }
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        var padding = FocusPadding;
        _ring.Measure(new Size(size.Width + padding * 2, size.Height + padding * 2));
        _ring.Arrange(new Rect(-padding, -padding, size.Width + padding * 2, size.Height + padding * 2));
        return size;
    }
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        Owner is not null ? new SliderThumbAutomationPeer(this) : base.OnCreateAutomationPeer();
}
