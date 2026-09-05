using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class AnimatedSize : ContentControl, ITransitionTrack
{
    private readonly DoubleVectorConverter _converter = new();
    private readonly Animatable<double> _width = new(0, new DoubleVectorConverter());
    private readonly Animatable<double> _height = new(0, new DoubleVectorConverter());
    private AvaloniaMotionHost? _host;
    private IDisposable? _clockRegistration;
    private bool _applyingAnimation;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(220);
    public LayoutSnapshot Snapshot => LayoutSnapshot.Capture(this);
    public IAnimationClock? Clock { get; private set; }

    public void AttachClock(IAnimationClock? clock)
    {
        if (ReferenceEquals(Clock, clock)) return;
        _clockRegistration?.Dispose(); _host?.Dispose();
        _clockRegistration = null; _host = null; Clock = clock;
        if (clock is not null)
            _clockRegistration = clock.Register(TickTrack, () => ((ITransitionTrack)this).IsRunning);
        else if (TopLevel.GetTopLevel(this) is { } top)
        {
            _host = new AvaloniaMotionHost(top);
            _host.Register(_width); _host.Register(_height);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Clock is null && TopLevel.GetTopLevel(this) is { } topLevel)
        {
            _host = new AvaloniaMotionHost(topLevel);
            _host.Register(_width); _host.Register(_height);
        }
        _width.ValueChanged += ApplyWidth;
        _height.ValueChanged += ApplyHeight;
        SizeChanged += OnSizeChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { SizeChanged -= OnSizeChanged; _width.ValueChanged -= ApplyWidth; _height.ValueChanged -= ApplyHeight; _host?.Dispose(); _clockRegistration?.Dispose(); _host = null; _clockRegistration = null; base.OnDetachedFromVisualTree(e); }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (_applyingAnimation)
            return;
        _width.AnimateTo(e.NewSize.Width, new TweenSpec<double>(_converter, Duration, Easings.FastOutSlowIn));
        _height.AnimateTo(e.NewSize.Height, new TweenSpec<double>(_converter, Duration, Easings.FastOutSlowIn));
        _host?.RequestFrame();
    }

    private void ApplyWidth(double value)
    {
        _applyingAnimation = true;
        try { Width = value; }
        finally { _applyingAnimation = false; }
    }

    private void ApplyHeight(double value)
    {
        _applyingAnimation = true;
        try { Height = value; }
        finally { _applyingAnimation = false; }
    }

    bool ITransitionTrack.IsRunning => _width.IsRunning || _height.IsRunning;
    bool ITransitionTrack.Tick(TimeSpan delta) => TickTrack(delta);
    void ITransitionTrack.Pause() { _width.Pause(); _height.Pause(); }
    void ITransitionTrack.Resume() { _width.Resume(); _height.Resume(); }
    void ITransitionTrack.Complete() { _width.Settle(); _height.Settle(); }
    void ITransitionTrack.Cancel() { _width.Stop(); _height.Stop(); }
    private bool TickTrack(TimeSpan delta)
    {
        var widthRunning = _width.Tick(delta);
        var heightRunning = _height.Tick(delta);
        return widthRunning || heightRunning;
    }
}
