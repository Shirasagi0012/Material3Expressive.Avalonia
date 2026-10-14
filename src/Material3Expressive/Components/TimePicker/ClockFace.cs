// Portions translated from AndroidX TimePicker.kt / AnalogTimePickerState / ClockDialNode.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Spring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

internal sealed class ClockFace : Control
{
    public static readonly StyledProperty<IBrush?> DialBrushProperty=AvaloniaProperty.Register<ClockFace,IBrush?>(nameof(DialBrush));
    public static readonly StyledProperty<IBrush?> SelectorBrushProperty=AvaloniaProperty.Register<ClockFace,IBrush?>(nameof(SelectorBrush));
    public static readonly StyledProperty<Spring> MotionProperty=AvaloniaProperty.Register<ClockFace,Spring>(nameof(Motion),new(0.9,700));
    public IBrush? DialBrush {get=>GetValue(DialBrushProperty);set=>SetValue(DialBrushProperty,value);}
    public IBrush? SelectorBrush {get=>GetValue(SelectorBrushProperty);set=>SetValue(SelectorBrushProperty,value);}
    public Spring Motion {get=>GetValue(MotionProperty);set=>SetValue(MotionProperty,value);}
    private readonly TimeEntryController _controller;
    private readonly List<Button> _labels=[];
    private AnimationChildScope? _scope;
    private Animatable<float>? _angle;
    private float _position;
    private bool _pressed, _dragged;
    private Point _start;
    private TimeSelection? _lastSelection;
    private bool _last24;
    static ClockFace(){FocusableProperty.OverrideDefaultValue<ClockFace>(true);AffectsRender<ClockFace>(DialBrushProperty,SelectorBrushProperty);}
    internal ClockFace(TimeEntryController controller)
    {
        _controller=controller;FlowDirection=FlowDirection.LeftToRight;
        AutomationProperties.SetName(this,"Time selection dial");
        AddHandler(PointerPressedEvent,Pressed,RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent,Moved,RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent,Released,RoutingStrategies.Tunnel);
        AnimationScope.AddScopeChangedHandler(this, (_, e) =>
        {
            if (e.NewScope is { } scope && this.IsAttachedToVisualTree())
            {
                BindScope(scope);
                Update();
            }
        });
        _position=TargetAngle;
    }
    private float TargetAngle => (_controller.Selection==TimeSelection.Hour?_controller.Value.Hour%12/12f:_controller.Value.Minute/60f)*MathF.Tau-MathF.PI/2;
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
    private double Radius => (_controller.Is24Hour && _controller.Selection==TimeSelection.Hour && _controller.IsPm?69:101)*Bounds.Width/256;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _angle=new Animatable<float>(new FloatConverter(),TargetAngle,VisibilityThresholds.Float);
        BindScope(AnimationScope.Get(this));Update();
    }
    private void BindScope(AnimationScope scope)
    {
        _scope?.Dispose();
        _scope=scope.CreateChild("Clock dial");
        _scope.SubscribeValue(_angle!,v=>{_position=v;InvalidateVisual();});
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _scope?.Dispose();_scope=null;_angle=null;_pressed=false;base.OnDetachedFromVisualTree(e);
    }
    internal void Update()
    {
        if(_lastSelection!=_controller.Selection || _last24!=_controller.Is24Hour)
        {
            foreach(var button in _labels){VisualChildren.Remove(button);LogicalChildren.Remove(button);}
            _labels.Clear();_lastSelection=_controller.Selection;_last24=_controller.Is24Hour;
            var count=_controller.Selection==TimeSelection.Hour&&_controller.Is24Hour?24:12;
            for(var i=0;i<count;i++)
            {
                var index=i;
                var value=ClockValue(i);
                var button=new Button {Content=value.ToString(),Width=48,Height=48,Padding=new(0)};
                button.Bind(StyledElement.ThemeProperty,new DynamicResourceExtension("MaterialClockLabelTheme"));
                AutomationProperties.SetName(button,$"{value} {(_controller.Selection==TimeSelection.Hour?"hours":"minutes")}");
                button.Click+=(_,_)=>
                {
                    if(_controller.Selection==TimeSelection.Minute)_controller.SetMinute(index*5);
                    else _controller.SetHour(_controller.Is24Hour?index:index%12+(_controller.IsPm?12:0));
                };
                _labels.Add(button);LogicalChildren.Add(button);VisualChildren.Add(button);
            }
            InvalidateMeasure();
        }
        for(var i=0;i<_labels.Count;i++)
            _labels[i].Classes.Set("selected",IsSelected(ClockValue(i)));
        Animate(TargetAngle,_pressed);InvalidateVisual();
    }
    private void Animate(float target,bool snap)
    {
        while(_position-target>MathF.PI)target+=MathF.Tau;
        while(_position-target<=-MathF.PI)target-=MathF.Tau;
        if(_scope is not { } scope || scope.Lifetime.IsCancellationRequested || _angle is null){_position=target;return;}
        var track=_angle;var motion=Motion;
        _= scope.Launch(async ct=>
        {
            if(snap)await track.SnapToAsync(target,cancellationToken:ct);
            else await track.AnimateToAsync(scope,target,new SpringSpec<float>(motion.DampingRatio,motion.Stiffness,(float)motion.VisibilityThreshold),cancellationToken:ct);
        },"Clock selector");
    }
    private void Pressed(object? sender,PointerPressedEventArgs e)
    {
        if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;
        _pressed=true;_dragged=false;_start=e.GetPosition(this);e.Pointer.Capture(this);Focus();e.Handled=true;
    }
    private void Moved(object? sender,PointerEventArgs e)
    {
        if(!_pressed)return;
        var point=e.GetPosition(this);
        if(((Vector)(point-_start)).Length>3)_dragged=true;
        if(_dragged)SetFromPoint(point,false);
        e.Handled=true;
    }
    private void Released(object? sender,PointerReleasedEventArgs e)
    {
        if(!_pressed)return;
        var wasHour=_controller.Selection==TimeSelection.Hour;
        SetFromPoint(e.GetPosition(this),!_dragged);_pressed=false;e.Pointer.Capture(null);Animate(TargetAngle,false);
        if(wasHour && _scope is { } scope)
            _=scope.Launch(async ct=>{await scope.DelayAsync(TimeSpan.FromMilliseconds(_dragged?0:100),ct);_controller.Select(TimeSelection.Minute);},"Clock advance to minute");
        e.Handled=true;
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e){_pressed=false;base.OnPointerCaptureLost(e);}
    private void SetFromPoint(Point point,bool tap)
    {
        var center=new Point(Bounds.Width/2,Bounds.Height/2);var delta=point-center;
        var angle=MathF.Atan2((float)delta.Y,(float)delta.X)+MathF.PI/2;
        if(angle<0)angle+=MathF.Tau;
        if(_controller.Selection==TimeSelection.Hour)
        {
            var hour=(int)MathF.Floor(angle/(MathF.Tau/12)+0.5f)%12;
            var pm=_controller.Is24Hour?((Vector)delta).Length<74*Bounds.Width/256:_controller.IsPm;
            _controller.SetHour(hour+(pm?12:0));
        }
        else
        {
            var step=tap?5:1;
            _controller.SetMinute(((int)MathF.Floor(angle/(MathF.Tau/60)/step+0.5f)*step)%60);
        }
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var delta=e.Key is Key.Up or Key.Right?1:e.Key is Key.Down or Key.Left?-1:0;
        if(delta!=0)
        {
            if(_controller.Selection==TimeSelection.Hour)_controller.SetHour((_controller.Value.Hour+delta+24)%24);
            else _controller.SetMinute((_controller.Value.Minute+delta+60)%60);
            e.Handled=true;
        }
        else if(e.Key is Key.Enter or Key.Space){_controller.Select(_controller.Selection==TimeSelection.Hour?TimeSelection.Minute:TimeSelection.Hour);e.Handled=true;}
    }
    protected override Size MeasureOverride(Size available){foreach(var b in _labels)b.Measure(new(48,48));return new(256,256);}
    protected override Size ArrangeOverride(Size size)
    {
        for(var i=0;i<_labels.Count;i++)
        {
            var angle=(i%12)*Math.PI/6-Math.PI/2;var radius=(i>=12?69:101)*size.Width/256;
            _labels[i].Arrange(new(size.Width/2+Math.Cos(angle)*radius-24,size.Height/2+Math.Sin(angle)*radius-24,48,48));
        }
        return size;
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var center=new Point(Bounds.Width/2,Bounds.Height/2);
        context.DrawEllipse(DialBrush,null,center,Bounds.Width/2,Bounds.Height/2);
        var handle=center+new Vector(Math.Cos(_position)*Radius,Math.Sin(_position)*Radius);
        context.DrawLine(new Pen(SelectorBrush,2),center,handle);
        context.DrawEllipse(SelectorBrush,null,center,4,4);
        context.DrawEllipse(SelectorBrush,null,handle,24*Bounds.Width/256,24*Bounds.Width/256);
    }
}

