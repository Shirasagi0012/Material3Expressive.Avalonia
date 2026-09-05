using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Media;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class AnimatedBounds : Decorator, ITransitionTrack
{
    private readonly PointVectorConverter _converter = new();
    private readonly Animatable<Point> _position = new(new Point(), new PointVectorConverter());
    private readonly Animatable<Size> _size = new(new Size(), new SizeVectorConverter());
    private AvaloniaMotionHost? _host;
    private IDisposable? _clockRegistration;
    public IAnimationClock? Clock { get; private set; }

    public void AttachClock(IAnimationClock? clock)
    {
        if (ReferenceEquals(Clock, clock)) return;
        _clockRegistration?.Dispose(); _host?.Dispose();
        _clockRegistration = null; _host = null; Clock = clock;
        if (clock is not null)
            _clockRegistration = clock.Register(((ITransitionTrack)this).Tick, () => ((ITransitionTrack)this).IsRunning);
        else if (TopLevel.GetTopLevel(this) is { } top)
        {
            _host = new AvaloniaMotionHost(top);
            _host.Register(_position); _host.Register(_size);
        }
    }
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(220);

    public static readonly StyledProperty<Rect> TargetBoundsProperty = AvaloniaProperty.Register<AnimatedBounds, Rect>(nameof(TargetBounds));
    public Rect TargetBounds { get => GetValue(TargetBoundsProperty); set => SetValue(TargetBoundsProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TargetBoundsProperty)
        {
            _position.AnimateTo(TargetBounds.Position, new TweenSpec<Point>(_converter, Duration, Easings.FastOutSlowIn));
            _size.AnimateTo(TargetBounds.Size, new TweenSpec<Size>(new SizeVectorConverter(), Duration, Easings.FastOutSlowIn));
            _host?.RequestFrame();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Clock is null && TopLevel.GetTopLevel(this) is { } topLevel)
        {
            _host = new AvaloniaMotionHost(topLevel);
            _host.Register(_position);
            _host.Register(_size);
        }
        _position.ValueChanged += ApplyPosition;
        _size.ValueChanged += ApplySize;
        if (TargetBounds != default)
        {
            _position.SnapTo(TargetBounds.Position);
            _size.SnapTo(TargetBounds.Size);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { _position.ValueChanged -= ApplyPosition; _size.ValueChanged -= ApplySize; _host?.Dispose(); _clockRegistration?.Dispose(); _host = null; _clockRegistration = null; base.OnDetachedFromVisualTree(e); }
    private void ApplyPosition(Point position) => RenderTransform = new TranslateTransform(position.X, position.Y);
    private void ApplySize(Size size) { Width = size.Width; Height = size.Height; }

    bool ITransitionTrack.IsRunning => _position.IsRunning || _size.IsRunning;
    bool ITransitionTrack.Tick(TimeSpan delta)
    {
        var positionRunning = _position.Tick(delta);
        var sizeRunning = _size.Tick(delta);
        return positionRunning || sizeRunning;
    }
    void ITransitionTrack.Pause() { _position.Pause(); _size.Pause(); }
    void ITransitionTrack.Resume() { _position.Resume(); _size.Resume(); }
    void ITransitionTrack.Complete() { _position.Settle(); _size.Settle(); }
    void ITransitionTrack.Cancel() { _position.Stop(); _size.Stop(); }
}
