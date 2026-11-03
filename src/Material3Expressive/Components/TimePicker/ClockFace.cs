// Portions translated from AndroidX TimePicker.kt / AnalogTimePickerState / ClockDialNode.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
using Avalonia;
using static Material3Expressive.Tokens.MaterialTokens;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;
using Spring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

internal sealed partial class ClockFace : Control
{
    public static readonly StyledProperty<double> DialSizeProperty = AvaloniaProperty.Register<ClockFace, double>(nameof(DialSize), 256d);
    public double DialSize { get => GetValue(DialSizeProperty); set => SetValue(DialSizeProperty, value); }
    public static readonly StyledProperty<double> LabelSizeProperty = AvaloniaProperty.Register<ClockFace, double>(nameof(LabelSize), 48d);
    public double LabelSize { get => GetValue(LabelSizeProperty); set => SetValue(LabelSizeProperty, value); }
    public static readonly StyledProperty<double> CenterSizeProperty = AvaloniaProperty.Register<ClockFace, double>(nameof(CenterSize), 8d);
    public double CenterSize { get => GetValue(CenterSizeProperty); set => SetValue(CenterSizeProperty, value); }
    public static readonly StyledProperty<double> HandleSizeProperty = AvaloniaProperty.Register<ClockFace, double>(nameof(HandleSize), 48d);
    public double HandleSize { get => GetValue(HandleSizeProperty); set => SetValue(HandleSizeProperty, value); }
    public static readonly StyledProperty<IBrush?> DialBrushProperty=AvaloniaProperty.Register<ClockFace,IBrush?>(nameof(DialBrush));
    public static readonly StyledProperty<IBrush?> SelectorBrushProperty=AvaloniaProperty.Register<ClockFace,IBrush?>(nameof(SelectorBrush));
    public static readonly StyledProperty<Spring> MotionProperty=AvaloniaProperty.Register<ClockFace,Spring>(nameof(Motion),new(0.9,700));
    public IBrush? DialBrush {get=>GetValue(DialBrushProperty);set=>SetValue(DialBrushProperty,value);}
    public IBrush? SelectorBrush {get=>GetValue(SelectorBrushProperty);set=>SetValue(SelectorBrushProperty,value);}
    public Spring Motion {get=>GetValue(MotionProperty);set=>SetValue(MotionProperty,value);}
    private readonly TimeEntryController _controller;
    private List<Button> _labels = [];
    internal IReadOnlyList<Button> Labels => _labels;
    private readonly ClockSelectorOverlay _selectorOverlay;
    private AnimationChildScope? _scope;
    private Animatable<float>? _angle;
    private readonly MutatorMutex _angleMutex = new();
    private float _position;
    private float _hourAngle, _minuteAngle;
    private TimeOnly _observedTime;
    private TimeSelection _committedSelection;
    private bool _selectionCommitPending;
    private TimeSelection? _lastSelection;
    private bool _last24;
    static ClockFace(){AffectsRender<ClockFace>(DialBrushProperty,SelectorBrushProperty,CenterSizeProperty,HandleSizeProperty);AffectsMeasure<ClockFace>(DialSizeProperty,LabelSizeProperty);}
    internal ClockFace(TimeEntryController controller)
    {
        _controller=controller;FlowDirection=FlowDirection.LeftToRight;
        _selectorOverlay = new ClockSelectorOverlay(this) { IsHitTestVisible = false };
        VisualChildren.Add(_selectorOverlay);
        TimeEntryController.BindDimension(this, DialSizeProperty, Comp.TimePicker.ClockDialContainerSize);
        TimeEntryController.BindDimension(this, LabelSizeProperty, Comp.TimePicker.ClockDialLabelContainerSize);
        TimeEntryController.BindDimension(this, CenterSizeProperty, Comp.TimePicker.ClockDialSelectorCenterContainerSize);
        TimeEntryController.BindDimension(this, HandleSizeProperty, Comp.TimePicker.ClockDialSelectorHandleContainerSize);
        AutomationProperties.SetName(this,"Time selection dial");
        AddHandler(PointerPressedEvent,Pressed,RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent,Moved,RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent,Released,RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, DialKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, DialKeyUp, RoutingStrategies.Tunnel);
        AnimationScope.AddScopeChangedHandler(this, (_, e) =>
        {
            if (e.NewScope is { } scope && this.IsAttachedToVisualTree())
            {
                BindScope(scope);
                Update();
            }
        });
        _observedTime = _controller.Value;
        _hourAngle = RadiansPerHour * (_observedTime.Hour % 12) - MathF.Tau / 4;
        _minuteAngle = RadiansPerMinute * _observedTime.Minute - MathF.Tau / 4;
        _position=TargetAngle;
    }
    private float TargetAngle => _controller.Selection == TimeSelection.Hour ? _hourAngle : _minuteAngle;
    internal float CurrentAngle => _position;
    private int ClockValue(int index)
    {
        if (_controller.Selection == TimeSelection.Minute) return index * 5;
        if (!_controller.Is24Hour) return index == 0 ? 12 : index;
        return index == 0 ? 0 : index;
    }
    private bool IsSelected(int value)
    {
        if (_controller.Selection == TimeSelection.Minute)
        {
            var closest = (int)Math.Round(_controller.Value.Minute / 5d, MidpointRounding.AwayFromZero) * 5 % 60;
            return value == closest;
        }
        return _controller.Is24Hour ? value == _controller.Value.Hour : value == (_controller.Value.Hour % 12 == 0 ? 12 : _controller.Value.Hour % 12);
    }
    private float SelectorRadius => (float)HandleSize / 2 * (float)Bounds.Width / 256;
    private Point SelectorPosition
    {
        get
        {
            var diameter = (float)Bounds.Width;
            var ratio = _controller.Is24Hour && _controller.Selection == TimeSelection.Hour && _controller.IsPm ? 69f / 256 : 101f / 256;
            var length = Math.Max(diameter * ratio - SelectorRadius, 0) + SelectorRadius;
            return new(length * MathF.Cos(_position) + diameter / 2, length * MathF.Sin(_position) + diameter / 2);
        }
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _ = MaterialInputMode.For(this);
        _committedSelection = _controller.Selection;
        _angle=new Animatable<float>(new FloatConverter(),TargetAngle,VisibilityThresholds.Float);
        BindScope(AnimationScope.Get(this));Update();
    }
    private void BindScope(AnimationScope scope)
    {
        _scope?.Dispose();
        _selectionCommitPending = false;
        _scope=scope.CreateChild("Clock dial");
        if (_labelTransition is not null)
        {
            if (_crossfade is { } host)
            {
                _ = AnimationScope.Get(host);
                // This component owns the parent loop. Reconnect its mapped child after
                // an explicit scope end, preserving the parent transition's play time.
                host.Source = ContentSource.From(_labelTransition, screen => screen == TimeSelection.Hour ? HourScreen : MinuteScreen);
            }
            StartLabelTransition(false);
        }
        _scope.SubscribeValue(_angle!,v=>{_position=v;UpdateLabelSelection();InvalidateVisual();_selectorOverlay.InvalidateVisual();});
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelPointer();
        _selectionCommitPending = false;
        _scope?.Dispose();_scope=null;_angle=null; ReleaseLabels(); base.OnDetachedFromVisualTree(e);
    }
    internal void Update()
    {
        if (_observedTime != _controller.Value)
        {
            if (!_pointerUpdating)
            {
                if (_observedTime.Hour != _controller.Value.Hour)
                    _hourAngle = RadiansPerHour * (_controller.Value.Hour % 12) - MathF.Tau / 4;
                if (_observedTime.Minute != _controller.Value.Minute)
                    _minuteAngle = RadiansPerMinute * _controller.Value.Minute - MathF.Tau / 4;
            }
            _observedTime = _controller.Value;
        }
        var previousSelected = _labels.FindIndex(label => label.Classes.Contains("selected"));
        var selectionChanged = _lastSelection != _controller.Selection || _last24 != _controller.Is24Hour;
        UpdateLabels();
        for(var i=0;i<_labels.Count;i++)
            _labels[i].Classes.Set("selected",IsSelected(ClockValue(i)));
        if (_dialFocusable && (selectionChanged || previousSelected != _labels.FindIndex(label => label.Classes.Contains("selected"))))
            Activate();
        if (!_pointerUpdating)
        {
            if (_committedSelection != _controller.Selection && _scope is { } scope)
            {
                if (!_selectionCommitPending)
                {
                    _selectionCommitPending = true;
                    _ = scope.Launch(async ct =>
                    {
                        await scope.NextFrameAsync(ct);
                        await Task.Yield(); ct.ThrowIfCancellationRequested();
                        _selectionCommitPending = false;
                        if (_committedSelection == _controller.Selection) return;
                        _committedSelection = _controller.Selection;
                        Animate(TargetAngle);
                    }, "Clock selection composition");
                }
            }
            else Animate(TargetAngle);
        }
        UpdateLabelSelection();
        InvalidateVisual();
        _selectorOverlay.InvalidateVisual();
    }
    private void Animate(float target, bool force = false)
    {
        if (!force && _angle is { } current && Normalize(current.TargetValue) == Normalize(target)
            && (current.IsRunning || Normalize(current.Value) == Normalize(target))) return;
        target = EndValueForAnimation(target);
        if(_scope is not { } scope || scope.Lifetime.IsCancellationRequested || _angle is null){_position=target;return;}
        var track=_angle;var motion=Motion;
        _= scope.Launch(ct => _angleMutex.MutateAsync(async token=>
        {
            await track.AnimateToAsync(scope,target,new SpringSpec<float>(motion.DampingRatio,motion.Stiffness,(float)motion.VisibilityThreshold),cancellationToken:token);
            return true;
        }, MutatePriority.PreventUserInput, ct),"Clock selector");
    }
    private static float Normalize(float angle)
    {
        // Kotlin normalizes against Double PI, then converts the result back to Float.
        var value = angle % (2 * Math.PI);
        return (float)(value < 0 ? value + 2 * Math.PI : value);
    }
    protected override Size MeasureOverride(Size available)
    {
        _crossfade?.Measure(new Size(DialSize, DialSize));
        return new(DialSize, DialSize);
    }
    protected override Size ArrangeOverride(Size size)
    {
        _crossfade?.Arrange(new Rect(size));
        _selectorOverlay.Arrange(new Rect(size));
        return size;
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var center=new Point(Bounds.Width/2,Bounds.Height/2);
        context.DrawEllipse(DialBrush,null,center,Bounds.Width/2,Bounds.Height/2);
        DrawSelector(context, beforeContent: true);
    }
}

