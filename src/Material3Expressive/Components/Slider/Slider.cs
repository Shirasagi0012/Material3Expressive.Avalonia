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

/// <summary>Material3 single-value slider. Explicit State owns the values; otherwise Value is two-way bindable.</summary>
public class Slider : TemplatedControl
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<SliderState?> StateProperty = AvaloniaProperty.Register<Slider, SliderState?>(nameof(State), null);
    public SliderState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public static readonly StyledProperty<float> ValueProperty = AvaloniaProperty.Register<Slider, float>(nameof(Value), 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public float Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public static readonly StyledProperty<float> MinimumProperty = AvaloniaProperty.Register<Slider, float>(nameof(Minimum), 0);
    public float Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public static readonly StyledProperty<float> MaximumProperty = AvaloniaProperty.Register<Slider, float>(nameof(Maximum), 1);
    public float Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public static readonly StyledProperty<int> StepsProperty = AvaloniaProperty.Register<Slider, int>(nameof(Steps), 0);
    public int Steps { get => GetValue(StepsProperty); set => SetValue(StepsProperty, value); }
    public static readonly StyledProperty<SliderColors?> ColorsProperty = AvaloniaProperty.Register<Slider, SliderColors?>(nameof(Colors), null);
    public SliderColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public static readonly StyledProperty<double> ThumbWidthProperty = AvaloniaProperty.Register<Slider, double>(nameof(ThumbWidth), 4);
    public double ThumbWidth { get => GetValue(ThumbWidthProperty); set => SetValue(ThumbWidthProperty, value); }
    public static readonly StyledProperty<double> ThumbHeightProperty = AvaloniaProperty.Register<Slider, double>(nameof(ThumbHeight), 44);
    public double ThumbHeight { get => GetValue(ThumbHeightProperty); set => SetValue(ThumbHeightProperty, value); }
    public static readonly StyledProperty<object?> ThumbProperty = AvaloniaProperty.Register<Slider, object?>(nameof(Thumb), null);
    public object? Thumb { get => GetValue(ThumbProperty); set => SetValue(ThumbProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> ThumbTemplateProperty = AvaloniaProperty.Register<Slider, IDataTemplate?>(nameof(ThumbTemplate), null);
    public IDataTemplate? ThumbTemplate { get => GetValue(ThumbTemplateProperty); set => SetValue(ThumbTemplateProperty, value); }
    public static readonly StyledProperty<object?> TrackProperty = AvaloniaProperty.Register<Slider, object?>(nameof(Track), null);
    public object? Track { get => GetValue(TrackProperty); set => SetValue(TrackProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> TrackTemplateProperty = AvaloniaProperty.Register<Slider, IDataTemplate?>(nameof(TrackTemplate), null);
    public IDataTemplate? TrackTemplate { get => GetValue(TrackTemplateProperty); set => SetValue(TrackTemplateProperty, value); }

    public static readonly DirectProperty<Slider, SliderState> ActualStateProperty = AvaloniaProperty.RegisterDirect<Slider, SliderState>(nameof(ActualState), s => s.ActualState);
    private SliderState _actualState = new();
    public SliderState ActualState => _actualState;
    public event EventHandler? ValueChangeFinished;
    internal bool Vertical => this is VerticalSlider;
    public bool IsVertical => Vertical;
    private ContentPresenter? _thumb, _track;
    private Control? _defaultThumb, _defaultTrack;
    private bool _syncing, _subscribed;
    private IPointer? _pointer;
    private Point _down, _last;
    private float _slop;
    private bool _dragging;
    public static readonly DirectProperty<Slider, InteractionState> InteractionProperty = AvaloniaProperty.RegisterDirect<Slider, InteractionState>(nameof(Interaction), s => s.Interaction);
    private InteractionState _interaction;
    /// <summary>Read-only feedback for custom thumb content using the shared interaction flags.</summary>
    public InteractionState Interaction => _interaction;
    static Slider()
    {
        FocusableProperty.OverrideDefaultValue<Slider>(true);
        AffectsMeasure<Slider>(ThumbWidthProperty, ThumbHeightProperty, ThumbProperty, ThumbTemplateProperty, TrackProperty, TrackTemplateProperty);
    }
    public Slider()
    {
        // Consume at the ancestor before Avalonia's source-level pointer focus policy.
        // Compose's slider gestures do not request focus; keyboard focus still works.
        AddHandler(PointerPressedEvent, (_, e) => OnPointerPressed(e), Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, (_, e) => OnPointerReleased(e), Avalonia.Interactivity.RoutingStrategies.Tunnel);
        GotFocus += (_, _) => SetInteraction(InteractionState.Focused, true);
        LostFocus += (_, _) => SetInteraction(InteractionState.Focused, false);
    }
    private void ConfigureState(bool rebuild = false)
    {
        if (_syncing || State is null && Minimum > Maximum) return;
        var next = State ?? (rebuild ? new SliderState(Value, Steps, Minimum, Maximum) : _actualState);
        if (State is null)
        {
            next.OnValueChange = value =>
            {
                _syncing = true;
                try { SetCurrentValue(ValueProperty, value); next.Value = Value; }
                finally { _syncing = false; }
            };
            next.Value = Value;
        }
        next.IsVertical = Vertical;
        next.IsRtl = FlowDirection == FlowDirection.RightToLeft;
        next.ReverseVerticalDirection = this is VerticalSlider { TopToBottom: false };
        if (!ReferenceEquals(next, _actualState))
        {
            CancelGesture(); Unsubscribe();
            SetAndRaise(ActualStateProperty, ref _actualState, next);
            Subscribe(); RefreshSlots();
        }
        InvalidateMeasure(); RefreshVisuals();
    }
    private void Subscribe()
    {
        if (_subscribed || !this.IsAttachedToVisualTree()) return;
        _actualState.PropertyChanged += StateChanged; _subscribed = true;
    }
    private void Unsubscribe() { if (_subscribed) _actualState.PropertyChanged -= StateChanged; _subscribed = false; }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SliderState.ValueProperty && Avalonia.Automation.Peers.ControlAutomationPeer.FromElement(this) is SliderAutomationPeer peer)
            peer.NotifyValue((float)e.OldValue!, (float)e.NewValue!);
        (_thumb?.GetVisualParent() as SliderLayout)?.InvalidateArrange();
        InvalidateArrange(); RefreshVisuals();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); ConfigureState(State is null); Subscribe(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { CancelGesture(); Unsubscribe(); base.OnDetachedFromVisualTree(e); }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CancelGesture(); base.OnApplyTemplate(e);
        _thumb = e.NameScope.Find<ContentPresenter>("PART_Thumb"); _track = e.NameScope.Find<ContentPresenter>("PART_Track");
        _defaultThumb = _thumb?.Content as Control; _defaultTrack = _track?.Content as Control;
        if (_thumb?.GetVisualParent() is InputElement capture)
            capture.PointerCaptureLost += (_, args) => { if (_pointer == args.Pointer) CancelGesture(); };
        ConfigureState(State is null); RefreshSlots();
    }
    private void RefreshSlots()
    {
        if (_thumb is not null) { _thumb.Content = Thumb ?? (ThumbTemplate is not null ? ActualState : _defaultThumb); _thumb.ContentTemplate = ThumbTemplate; }
        if (_track is not null) { _track.Content = Track ?? (TrackTemplate is not null ? ActualState : _defaultTrack); _track.ContentTemplate = TrackTemplate; }
    }
    private void SetInteraction(InteractionState flag, bool active)
    {
        SetAndRaise(InteractionProperty, ref _interaction, active ? Interaction | flag : Interaction & ~flag);
        RefreshVisuals();
    }
    private void RefreshVisuals()
    {
        if (_defaultThumb is SliderThumb thumb) { thumb.Interaction = Interaction; thumb.IsVertical = Vertical; }
        _defaultTrack?.InvalidateVisual();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == ActualStateProperty && e.OldValue is SliderState oldState && e.NewValue is SliderState newState
            && Avalonia.Automation.Peers.ControlAutomationPeer.FromElement(this) is SliderAutomationPeer peer) peer.NotifyState(oldState, newState);
        if (e.Property == StateProperty || e.Property == MinimumProperty || e.Property == MaximumProperty || e.Property == StepsProperty)
            ConfigureState(true);
        else if (e.Property == ValueProperty || e.Property == FlowDirectionProperty || e.Property == VerticalSlider.TopToBottomProperty) ConfigureState();
        else if (e.Property == ThumbProperty || e.Property == ThumbTemplateProperty || e.Property == TrackProperty || e.Property == TrackTemplateProperty) RefreshSlots();
        else if (e.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) CancelGesture();
    }
    private float Scale => (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEffectivelyEnabled || _pointer is not null || e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _pointer = e.Pointer; _down = _last = e.GetPosition(this); _slop = e.Pointer.Type == PointerType.Mouse ? 0.125f : 18f;
        ActualState.Press((float)_down.X * Scale, (float)_down.Y * Scale);
        SetInteraction(InteractionState.Pressed, true);
        e.Pointer.Capture(_thumb?.GetVisualParent() as InputElement ?? this); e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointer != e.Pointer) return;
        var position = e.GetPosition(this); var total = position - _down;
        var axis = (float)(Vertical ? total.Y : total.X);
        var reverse = Vertical ? ActualState.ReverseVerticalDirection : ActualState.IsRtl;
        if (!_dragging && MathF.Abs(axis) >= _slop)
        {
            _dragging = ActualState.IsDragging = true;
            SetInteraction(InteractionState.Pressed, false); SetInteraction(InteractionState.Dragged, true);
            ActualState.DispatchRawDelta((axis - MathF.CopySign(_slop, axis)) * Scale * (reverse ? -1 : 1));
        }
        else if (_dragging)
            ActualState.DispatchRawDelta((float)(Vertical ? position.Y - _last.Y : position.X - _last.X) * Scale * (reverse ? -1 : 1));
        _last = position; e.Handled = true;
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointer != e.Pointer) return;
        var pointer = _pointer; _pointer = null;
        if (!_dragging) ActualState.DispatchRawDelta(0);
        _dragging = ActualState.IsDragging = false;
        SetInteraction(InteractionState.Pressed | InteractionState.Dragged, false); Finish(); pointer.Capture(null); e.Handled = true;
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { base.OnPointerCaptureLost(e); if (_pointer == e.Pointer) CancelGesture(); }
    private void CancelGesture()
    {
        if (_pointer is null) return;
        var pointer = _pointer; _pointer = null; var dragging = _dragging;
        _dragging = ActualState.IsDragging = false;
        SetInteraction(InteractionState.Pressed | InteractionState.Dragged, false);
        if (dragging) Finish(); pointer.Capture(null);
    }
    internal void Finish() { ActualState.Finish(); ValueChangeFinished?.Invoke(this, EventArgs.Empty); }
    internal void SetProgress(float value) { if (ActualState.SetProgress(value)) ValueChangeFinished?.Invoke(this, EventArgs.Empty); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (IsEffectivelyEnabled && SliderMath.KeyValue(e.Key, Vertical, Vertical ? ActualState.ReverseVerticalDirection : ActualState.IsRtl,
            ActualState.Value, ActualState.Minimum, ActualState.Maximum, ActualState.Minimum, ActualState.Maximum, ActualState.Steps, out var value))
        { ActualState.Change(value); e.Handled = true; }
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (IsEffectivelyEnabled && SliderMath.KeyValue(e.Key, Vertical, false, ActualState.Value, ActualState.Minimum, ActualState.Maximum,
            ActualState.Minimum, ActualState.Maximum, ActualState.Steps, out _)) { Finish(); e.Handled = true; }
    }
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new SliderAutomationPeer(this);
}

/// <summary>Vertical Slider; increasing values run top to bottom unless TopToBottom=false.</summary>
public sealed class VerticalSlider : Slider
{
    public static readonly StyledProperty<bool> TopToBottomProperty = AvaloniaProperty.Register<VerticalSlider, bool>(nameof(TopToBottom), true);
    public bool TopToBottom { get => GetValue(TopToBottomProperty); set => SetValue(TopToBottomProperty, value); }

    protected override Type StyleKeyOverride => typeof(VerticalSlider);
}
