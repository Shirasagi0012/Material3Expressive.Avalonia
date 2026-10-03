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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

/// <summary>Two independently focusable Material3 thumbs with a shared, non-crossing active range.</summary>
public sealed class RangeSlider : TemplatedControl
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<RangeSliderState?> StateProperty = AvaloniaProperty.Register<RangeSlider, RangeSliderState?>(nameof(State), null);
    public RangeSliderState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public static readonly StyledProperty<float> RangeStartProperty = AvaloniaProperty.Register<RangeSlider, float>(nameof(RangeStart), 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public float RangeStart { get => GetValue(RangeStartProperty); set => SetValue(RangeStartProperty, value); }
    public static readonly StyledProperty<float> RangeEndProperty = AvaloniaProperty.Register<RangeSlider, float>(nameof(RangeEnd), 1, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public float RangeEnd { get => GetValue(RangeEndProperty); set => SetValue(RangeEndProperty, value); }
    public static readonly StyledProperty<float> MinimumProperty = AvaloniaProperty.Register<RangeSlider, float>(nameof(Minimum), 0);
    public float Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public static readonly StyledProperty<float> MaximumProperty = AvaloniaProperty.Register<RangeSlider, float>(nameof(Maximum), 1);
    public float Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public static readonly StyledProperty<int> StepsProperty = AvaloniaProperty.Register<RangeSlider, int>(nameof(Steps), 0);
    public int Steps { get => GetValue(StepsProperty); set => SetValue(StepsProperty, value); }
    public static readonly StyledProperty<SliderColors?> ColorsProperty = AvaloniaProperty.Register<RangeSlider, SliderColors?>(nameof(Colors), null);
    public SliderColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public static readonly StyledProperty<double> ThumbWidthProperty = AvaloniaProperty.Register<RangeSlider, double>(nameof(ThumbWidth), 4);
    public double ThumbWidth { get => GetValue(ThumbWidthProperty); set => SetValue(ThumbWidthProperty, value); }
    public static readonly StyledProperty<double> ThumbHeightProperty = AvaloniaProperty.Register<RangeSlider, double>(nameof(ThumbHeight), 44);
    public double ThumbHeight { get => GetValue(ThumbHeightProperty); set => SetValue(ThumbHeightProperty, value); }
    public static readonly StyledProperty<object?> StartThumbProperty = AvaloniaProperty.Register<RangeSlider, object?>(nameof(StartThumb), null);
    public object? StartThumb { get => GetValue(StartThumbProperty); set => SetValue(StartThumbProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> StartThumbTemplateProperty = AvaloniaProperty.Register<RangeSlider, IDataTemplate?>(nameof(StartThumbTemplate), null);
    public IDataTemplate? StartThumbTemplate { get => GetValue(StartThumbTemplateProperty); set => SetValue(StartThumbTemplateProperty, value); }
    public static readonly StyledProperty<object?> EndThumbProperty = AvaloniaProperty.Register<RangeSlider, object?>(nameof(EndThumb), null);
    public object? EndThumb { get => GetValue(EndThumbProperty); set => SetValue(EndThumbProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> EndThumbTemplateProperty = AvaloniaProperty.Register<RangeSlider, IDataTemplate?>(nameof(EndThumbTemplate), null);
    public IDataTemplate? EndThumbTemplate { get => GetValue(EndThumbTemplateProperty); set => SetValue(EndThumbTemplateProperty, value); }
    public static readonly StyledProperty<object?> TrackProperty = AvaloniaProperty.Register<RangeSlider, object?>(nameof(Track), null);
    public object? Track { get => GetValue(TrackProperty); set => SetValue(TrackProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> TrackTemplateProperty = AvaloniaProperty.Register<RangeSlider, IDataTemplate?>(nameof(TrackTemplate), null);
    public IDataTemplate? TrackTemplate { get => GetValue(TrackTemplateProperty); set => SetValue(TrackTemplateProperty, value); }

    public static readonly DirectProperty<RangeSlider, RangeSliderState> ActualStateProperty = AvaloniaProperty.RegisterDirect<RangeSlider, RangeSliderState>(nameof(ActualState), s => s.ActualState);
    private RangeSliderState _actualState = new();
    public RangeSliderState ActualState => _actualState;
    public event EventHandler? ValueChangeFinished;
    internal SliderThumbPresenter? StartPresenter, EndPresenter;
    private ContentPresenter? _track;
    private Control? _defaultStart, _defaultEnd, _defaultTrack;
    private bool _syncing, _subscribed;
    private IPointer? _pointer;
    private Point _down, _last;
    private float _pressX, _slop;
    private bool _dragging, _draggingStart;
    private InteractionState _startInteraction, _endInteraction;
    public static readonly DirectProperty<RangeSlider, InteractionState> StartInteractionProperty = AvaloniaProperty.RegisterDirect<RangeSlider, InteractionState>(nameof(StartInteraction), s => s.StartInteraction);
    public static readonly DirectProperty<RangeSlider, InteractionState> EndInteractionProperty = AvaloniaProperty.RegisterDirect<RangeSlider, InteractionState>(nameof(EndInteraction), s => s.EndInteraction);
    public InteractionState StartInteraction => _startInteraction;
    public InteractionState EndInteraction => _endInteraction;
    static RangeSlider() => AffectsMeasure<RangeSlider>(ThumbWidthProperty, ThumbHeightProperty, StartThumbProperty, StartThumbTemplateProperty, EndThumbProperty, EndThumbTemplateProperty, TrackProperty, TrackTemplateProperty);
    public RangeSlider()
    {
        AddHandler(PointerPressedEvent, (_, e) => OnPointerPressed(e), Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, (_, e) => OnPointerReleased(e), Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }
    private void ConfigureState(bool rebuild = false)
    {
        if (_syncing || State is null && Minimum > Maximum) return;
        var next = State ?? (rebuild ? new RangeSliderState(RangeStart, RangeEnd, Steps, Minimum, Maximum) : _actualState);
        if (State is null)
        {
            next.OnValueChange = value =>
            {
                _syncing = true;
                try { SetCurrentValue(RangeStartProperty, value.Start); SetCurrentValue(RangeEndProperty, value.EndInclusive); next.ActiveRangeStart = RangeStart; next.ActiveRangeEnd = RangeEnd; }
                finally { _syncing = false; }
            };
            next.ActiveRangeStart = RangeStart; next.ActiveRangeEnd = RangeEnd;
        }
        next.IsRtl = FlowDirection == FlowDirection.RightToLeft;
        if (!ReferenceEquals(next, _actualState)) { CancelGesture(); Unsubscribe(); SetAndRaise(ActualStateProperty, ref _actualState, next); Subscribe(); RefreshSlots(); }
        InvalidateMeasure(); RefreshVisuals();
    }
    private void Subscribe() { if (_subscribed || !this.IsAttachedToVisualTree()) return; _actualState.PropertyChanged += StateChanged; _subscribed = true; }
    private void Unsubscribe() { if (_subscribed) _actualState.PropertyChanged -= StateChanged; _subscribed = false; }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == RangeSliderState.ActiveRangeStartProperty || e.Property == RangeSliderState.ActiveRangeEndProperty)
            foreach (var thumb in new[] { StartPresenter, EndPresenter })
                if (thumb is not null && Avalonia.Automation.Peers.ControlAutomationPeer.FromElement(thumb) is SliderThumbAutomationPeer peer)
                    peer.NotifyValueOrBound(e.Property == RangeSliderState.ActiveRangeStartProperty, (float)e.OldValue!, (float)e.NewValue!);
        (StartPresenter?.GetVisualParent() as SliderLayout)?.InvalidateArrange();
        InvalidateArrange(); RefreshVisuals();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); ConfigureState(State is null); Subscribe(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { CancelGesture(); Unsubscribe(); base.OnDetachedFromVisualTree(e); }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CancelGesture(); base.OnApplyTemplate(e);
        StartPresenter = e.NameScope.Find<SliderThumbPresenter>("PART_StartThumb"); EndPresenter = e.NameScope.Find<SliderThumbPresenter>("PART_EndThumb"); _track = e.NameScope.Find<ContentPresenter>("PART_Track");
        _defaultStart = StartPresenter?.Content as Control; _defaultEnd = EndPresenter?.Content as Control; _defaultTrack = _track?.Content as Control;
        if (StartPresenter?.GetVisualParent() is InputElement capture)
            capture.PointerCaptureLost += (_, args) => { if (_pointer == args.Pointer) CancelGesture(); };
        if (StartPresenter is { } start) { start.Owner = this; start.IsStart = true; start.GotFocus += OnThumbFocus; start.LostFocus += OnThumbFocus; }
        if (EndPresenter is { } end) { end.Owner = this; end.IsStart = false; end.GotFocus += OnThumbFocus; end.LostFocus += OnThumbFocus; }
        ConfigureState(State is null); RefreshSlots();
    }
    private void OnThumbFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not SliderThumbPresenter thumb) return;
        ref var interaction = ref (thumb.IsStart ? ref _startInteraction : ref _endInteraction);
        SetAndRaise(thumb.IsStart ? StartInteractionProperty : EndInteractionProperty, ref interaction,
            thumb.IsFocused ? interaction | InteractionState.Focused : interaction & ~InteractionState.Focused);
        RefreshVisuals();
    }
    private void RefreshSlots()
    {
        if (StartPresenter is { } start) { start.Content = StartThumb ?? (StartThumbTemplate is not null ? ActualState : _defaultStart); start.ContentTemplate = StartThumbTemplate; }
        if (EndPresenter is { } end) { end.Content = EndThumb ?? (EndThumbTemplate is not null ? ActualState : _defaultEnd); end.ContentTemplate = EndThumbTemplate; }
        if (_track is not null) { _track.Content = Track ?? (TrackTemplate is not null ? ActualState : _defaultTrack); _track.ContentTemplate = TrackTemplate; }
    }
    private void SetInteraction(InteractionState flag, bool active)
    {
        ref var interaction = ref (_draggingStart ? ref _startInteraction : ref _endInteraction);
        SetAndRaise(_draggingStart ? StartInteractionProperty : EndInteractionProperty, ref interaction, active ? interaction | flag : interaction & ~flag);
        RefreshVisuals();
    }
    private void RefreshVisuals()
    {
        if (_defaultStart is SliderThumb start) start.Interaction = _startInteraction;
        if (_defaultEnd is SliderThumb end) end.Interaction = _endInteraction;
        _defaultTrack?.InvalidateVisual();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == ActualStateProperty && e.OldValue is RangeSliderState oldState && e.NewValue is RangeSliderState newState)
            foreach (var thumb in new[] { StartPresenter, EndPresenter })
                if (thumb is not null && Avalonia.Automation.Peers.ControlAutomationPeer.FromElement(thumb) is SliderThumbAutomationPeer peer)
                    peer.NotifyState(oldState, newState);
        if (e.Property == StateProperty || e.Property == MinimumProperty || e.Property == MaximumProperty || e.Property == StepsProperty) ConfigureState(true);
        else if (e.Property == RangeStartProperty || e.Property == RangeEndProperty || e.Property == FlowDirectionProperty) ConfigureState();
        else if (e.Property == StartThumbProperty || e.Property == StartThumbTemplateProperty || e.Property == EndThumbProperty || e.Property == EndThumbTemplateProperty || e.Property == TrackProperty || e.Property == TrackTemplateProperty) RefreshSlots();
        else if (e.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) CancelGesture();
    }
    private float Scale => (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEffectivelyEnabled || _pointer is not null || e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _pointer = e.Pointer; _down = _last = e.GetPosition(this); _slop = e.Pointer.Type == PointerType.Mouse ? 0.125f : 18f;
        _pressX = ActualState.IsRtl ? ActualState.TotalWidth - (float)_down.X * Scale : (float)_down.X * Scale;
        var difference = MathF.Abs(ActualState.RawOffsetStart - _pressX).CompareTo(MathF.Abs(ActualState.RawOffsetEnd - _pressX));
        _draggingStart = difference != 0 ? difference < 0 : ActualState.RawOffsetStart > _pressX;
        SetInteraction(InteractionState.Pressed, true);
        e.Pointer.Capture(StartPresenter?.GetVisualParent() as InputElement ?? this); e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointer != e.Pointer) return;
        var position = e.GetPosition(this); var total = position - _down;
        if (!_dragging && total.X * total.X + total.Y * total.Y >= _slop * _slop)
        {
            if (Math.Abs(total.X) <= Math.Abs(total.Y)) { CancelGesture(); return; }
            SetInteraction(InteractionState.Pressed, false); SetInteraction(InteractionState.Dragged, true);
            var initial = _pressX - (_draggingStart ? ActualState.RawOffsetStart : ActualState.RawOffsetEnd);
            ActualState.Drag(_draggingStart, initial + (float)total.X * Scale * (ActualState.IsRtl ? -1 : 1));
            _dragging = ActualState.IsDragging = true;
        }
        else if (_dragging) ActualState.Drag(_draggingStart, (float)(position.X - _last.X) * Scale * (ActualState.IsRtl ? -1 : 1));
        _last = position; e.Handled = true;
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointer != e.Pointer) return;
        var pointer = _pointer; _pointer = null;
        if (!_dragging)
        {
            // The final up event is also part of rangeSliderPressDragModifier's slop loop.
            // A horizontal displacement on that event becomes an immediate completed drag.
            var total = e.GetPosition(this) - _down;
            var offset = _pressX - (_draggingStart ? ActualState.RawOffsetStart : ActualState.RawOffsetEnd);
            if (Math.Abs(total.X) > Math.Abs(total.Y)) offset += (float)total.X * Scale * (ActualState.IsRtl ? -1 : 1);
            ActualState.Drag(_draggingStart, offset);
        }
        _dragging = ActualState.IsDragging = false;
        SetInteraction(InteractionState.Pressed | InteractionState.Dragged, false); Finish(); pointer.Capture(null); e.Handled = true;
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { base.OnPointerCaptureLost(e); if (_pointer == e.Pointer) CancelGesture(); }
    private void CancelGesture()
    {
        if (_pointer is null) return;
        var pointer = _pointer; var state = ActualState; _pointer = null; var dragging = _dragging;
        _dragging = false; SetInteraction(InteractionState.Pressed | InteractionState.Dragged, false);
        // The upstream cancellation finally invokes gestureEndAction while its drag
        // guard still prevents reentrant layout from resetting the accumulated offsets.
        try { if (dragging) Finish(); }
        finally { state.IsDragging = false; pointer.Capture(null); }
    }
    internal void Finish() { ActualState.OnValueChangeFinished?.Invoke(); ValueChangeFinished?.Invoke(this, EventArgs.Empty); }
    internal void SetProgress(bool start, float value) { if (ActualState.SetProgress(start, value)) ValueChangeFinished?.Invoke(this, EventArgs.Empty); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var start = StartPresenter?.IsFocused == true;
        if (!IsEffectivelyEnabled || !start && EndPresenter?.IsFocused != true) return;
        if (SliderMath.KeyValue(e.Key, false, ActualState.IsRtl, start ? ActualState.ActiveRangeStart : ActualState.ActiveRangeEnd,
            start ? ActualState.Minimum : ActualState.ActiveRangeStart, start ? ActualState.ActiveRangeEnd : ActualState.Maximum,
            ActualState.Minimum, ActualState.Maximum, ActualState.Steps, out var value))
        { ActualState.Change(start ? new(value, ActualState.ActiveRangeEnd) : new(ActualState.ActiveRangeStart, value)); e.Handled = true; }
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (IsEffectivelyEnabled && (StartPresenter?.IsFocused == true || EndPresenter?.IsFocused == true)
            && SliderMath.KeyValue(e.Key, false, false, 0, ActualState.Minimum, ActualState.Maximum, ActualState.Minimum, ActualState.Maximum, ActualState.Steps, out _))
        { Finish(); e.Handled = true; }
    }
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new RangeSliderAutomationPeer(this);
}
