using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class AnimatedVisibility : ContentControl, ITransitionTrack
{
    private readonly VisibilityMotion _motion = new(false);
    private AvaloniaMotionHost? _host;
    private IDisposable? _clockRegistration;

    public AnimatedVisibility() => IsVisible = false;
    public static readonly StyledProperty<bool> IsShownProperty = AvaloniaProperty.Register<AnimatedVisibility, bool>(nameof(IsShown));
    public bool IsShown { get => GetValue(IsShownProperty); set => SetValue(IsShownProperty, value); }
    public EnterTransition Enter { get; set; } = EnterTransition.FadeIn();
    public ExitTransition Exit { get; set; } = ExitTransition.FadeOut();
    public double Progress => _motion.Progress;
    public IAnimationClock? Clock { get; private set; }

    public void AttachClock(IAnimationClock? clock)
    {
        if (ReferenceEquals(Clock, clock)) return;
        _clockRegistration?.Dispose();
        _host?.Dispose();
        _clockRegistration = null;
        _host = null;
        Clock = clock;
        if (clock is not null)
            _clockRegistration = clock.Register(Tick, () => _motion.IsRunning);
        else if (TopLevel.GetTopLevel(this) is { } top)
        {
            _host = new AvaloniaMotionHost(top);
            _host.RegisterTrack(Tick, () => _motion.IsRunning);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != IsShownProperty) return;
        if (IsShown) { IsVisible = true; _motion.Show(); } else _motion.Hide();
        _host?.RequestFrame();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Clock is null && TopLevel.GetTopLevel(this) is { } topLevel)
        {
            _host = new AvaloniaMotionHost(topLevel);
            _host.RegisterTrack(Tick, () => _motion.IsRunning);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _host?.Dispose();
        _clockRegistration?.Dispose();
        _host = null;
        _clockRegistration = null;
        base.OnDetachedFromVisualTree(e);
    }

    private bool Tick(TimeSpan delta)
    {
        var running = _motion.Tick(delta);
        if (_motion.State == VisibilityState.Visible) Enter.Apply(this, _motion.Progress);
        else Exit.Apply(this, 1 - _motion.Progress);
        if (!running)
        {
            TransformComposition.End(this);
            if (!IsShown) IsVisible = false;
        }
        return running;
    }

    bool ITransitionTrack.IsRunning => _motion.IsRunning;
    bool ITransitionTrack.Tick(TimeSpan delta) => Tick(delta);
    void ITransitionTrack.Pause() => _motion.Pause();
    void ITransitionTrack.Resume() => _motion.Resume();
    void ITransitionTrack.Complete() => _motion.Complete();
    void ITransitionTrack.Cancel() => _motion.Cancel();
}
