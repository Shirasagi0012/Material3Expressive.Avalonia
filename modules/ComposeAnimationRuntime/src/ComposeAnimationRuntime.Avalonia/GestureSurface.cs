using System.Numerics;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class GestureSurface : ContentControl
{
    private readonly GestureController _gesture = new();
    private readonly InteractiveTransition _interactive = new(TimeSpan.FromMilliseconds(220));
    private AvaloniaMotionHost? _host;
    private IPointer? _pointer;
    private long _startTimestamp;
    private SharedInteractiveTransition? _sharedTransition;
    private IDisposable? _sharedRegistration;

    public GestureSurface()
    {
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Tunnel);
        _gesture.PositionChanged += position => PositionChanged?.Invoke(this, position);
    }

    public Vector2 Position => _gesture.Position;
    public double Progress => _interactive.Progress;
    public Vector2 Minimum { get => _minimum; set { _minimum = value; _gesture.SetBounds(_minimum, _maximum); } }
    public Vector2 Maximum { get => _maximum; set { _maximum = value; _gesture.SetBounds(_minimum, _maximum); } }
    public bool SpringToBounds { get; set; } = true;
    public IProgressMapper ProgressMapper { get; set; } = new HorizontalProgressMapper();
    public event EventHandler<Vector2>? PositionChanged;
    public InteractiveTransition InteractiveTransition => _interactive;
    public SharedInteractiveTransition? SharedTransition => _sharedTransition;

    public void BindSharedTransition(SharedInteractiveTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        _sharedRegistration?.Dispose();
        _sharedTransition = transition;
        if (_host is not null)
            _sharedRegistration = _host.RegisterTrack(transition.Tick, () => transition.IsRunning);
        transition.SetProgress(Progress);
    }
    private Vector2 _minimum;
    private Vector2 _maximum = new(float.MaxValue);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is { } top)
        {
            _host = new AvaloniaMotionHost(top);
            _host.RegisterTrack(_gesture.Tick, () => _gesture.IsRunning);
            _host.RegisterTrack(_interactive.Tick, () => _interactive.IsRunning);
            if (_sharedTransition is not null)
                _host.RegisterTrack(_sharedTransition.Tick, () => _sharedTransition.IsRunning);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _pointer?.Capture(null);
        _gesture.Cancel();
        _host?.Dispose();
        _sharedRegistration?.Dispose();
        _sharedRegistration = null;
        _host = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        _pointer = e.Pointer;
        _pointer.Capture(this);
        _startTimestamp = Stopwatch.GetTimestamp();
        _gesture.Begin(ToVector(e.GetPosition(this)), TimeSpan.Zero);
        _interactive.SetProgress(0);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(_pointer, e.Pointer)) return;
        _gesture.Update(ToVector(e.GetPosition(this)), Stopwatch.GetElapsedTime(_startTimestamp));
        _interactive.SetProgress(ProgressMapper.Map(_gesture.Position, Bounds.Size));
        _sharedTransition?.SetProgress(Progress);
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(_pointer, e.Pointer)) return;
        var target = SpringToBounds ? Vector2.Clamp(_gesture.Position, _minimum, _maximum) : (Vector2?)null;
        var velocity = _gesture.Velocity.X;
        _gesture.End(settleTarget: target);
        _interactive.Release(velocity);
        _sharedTransition?.Release(velocity);
        _pointer.Capture(null);
        _pointer = null;
        _host?.RequestFrame();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_pointer is not null)
        {
            _gesture.Cancel();
            _interactive.Cancel();
            _sharedTransition?.Cancel();
        }
        _pointer = null;
    }

    private static Vector2 ToVector(Point point) => new((float)point.X, (float)point.Y);
}
