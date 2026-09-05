using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

/// <summary>Shows old and new content together while a content transform runs.</summary>
public sealed class AnimatedContent : ContentControl, ITransitionTrack
{
    private readonly DoubleVectorConverter _converter = new();
    private readonly Animatable<double> _progress = new(1, new DoubleVectorConverter());
    private readonly Grid _layers;
    private readonly ContentPresenter _currentPresenter;
    private readonly ContentPresenter _outgoingPresenter;
    private AvaloniaMotionHost? _host;
    private object? _pendingContent;
    private bool _hasPendingContent;
    private object? _currentKey;
    private object? _pendingKey;
    private LayoutSnapshot _previousLayout;
    private bool _transitioning;
    private IDisposable? _clockRegistration;

    public AnimatedContent()
    {
        _currentPresenter = new ContentPresenter();
        _outgoingPresenter = new ContentPresenter { IsVisible = false };
        _layers = new Grid();
        _layers.Children.Add(_outgoingPresenter);
        _layers.Children.Add(_currentPresenter);
        base.Content = _layers;
    }

    public static readonly StyledProperty<object?> TargetStateProperty = AvaloniaProperty.Register<AnimatedContent, object?>(nameof(TargetState));
    public object? TargetState { get => GetValue(TargetStateProperty); set => SetValue(TargetStateProperty, value); }
    [Obsolete("Use TargetState instead.")]
    public object? AnimatedValue { get => TargetState; set => TargetState = value; }
    public Func<object?, object?>? ContentFactory { get; set; }
    public Func<object?, object?>? KeySelector { get; set; }
    public IEqualityComparer<object?> KeyComparer { get; set; } = EqualityComparer<object?>.Default;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(220);
    public ContentTransform Transform { get; set; } = new(EnterTransition.FadeIn(), ExitTransition.FadeOut());
    public IAnimationClock? Clock { get; private set; }
    public LayoutSnapshot PreviousLayout => _previousLayout;
    public LayoutSnapshot CurrentLayout => LayoutSnapshot.Capture(_currentPresenter);
    public object? CurrentKey => _currentKey;
    public event EventHandler? TransitionCompleted;
    public bool IsTransitioning => _transitioning;

    public void AttachClock(IAnimationClock? clock)
    {
        if (ReferenceEquals(Clock, clock)) return;
        _clockRegistration?.Dispose();
        _clockRegistration = null;
        _host?.Dispose();
        _host = null;
        Clock = clock;
        if (clock is not null && (_progress.IsRunning || _transitioning))
            _clockRegistration = clock.Register(Tick, () => _progress.IsRunning || _transitioning);
        else if (clock is null && TopLevel.GetTopLevel(this) is { } top)
        {
            _host = new AvaloniaMotionHost(top);
            _host.RegisterTrack(Tick, () => _progress.IsRunning || _transitioning || _hasPendingContent);
        }
    }

    public void CompleteTransition()
    {
        if (!_transitioning)
            return;
        _progress.SnapTo(1);
        FinishTransition();
    }

    public void CancelTransition()
    {
        if (!_transitioning)
            return;
        _progress.Stop();
        ClearOutgoing();
        TransformComposition.End(_outgoingPresenter);
        TransformComposition.End(_currentPresenter);
        TransformComposition.End(this);
        _transitioning = false;
        _host?.RequestFrame();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != TargetStateProperty) return;
        _previousLayout = LayoutSnapshot.Capture(_currentPresenter);
        var state = change.NewValue;
        _pendingContent = ContentFactory?.Invoke(state) ?? state;
        _pendingKey = KeySelector?.Invoke(state) ?? state;
        _hasPendingContent = true;
        StartTransition();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            if (Clock is null)
            {
                _host = new AvaloniaMotionHost(topLevel);
                _host.RegisterTrack(Tick, () => _progress.IsRunning || _transitioning || _hasPendingContent);
            }
            else
                _clockRegistration = Clock.Register(Tick, () => _progress.IsRunning || _transitioning);
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

    private void StartTransition()
    {
        if (!_hasPendingContent) return;

        if (KeyComparer.Equals(_currentKey, _pendingKey))
        {
            _currentPresenter.Content = _pendingContent;
            _pendingContent = null;
            _hasPendingContent = false;
            _pendingKey = null;
            return;
        }

        ClearOutgoing();
        var outgoing = _currentPresenter.Content;
        _currentPresenter.Content = null;
        _outgoingPresenter.Content = outgoing;
        _outgoingPresenter.IsVisible = outgoing is not null;
        _currentPresenter.Content = _pendingContent;
        _pendingContent = null;
        _hasPendingContent = false;
        _currentKey = _pendingKey;
        _pendingKey = null;
        _progress.SnapTo(0);
        _transitioning = true;
        _host?.RequestFrame();
    }

    private bool Tick(TimeSpan delta)
    {
        if (!_transitioning) return false;
        if (!_progress.IsRunning)
            _progress.AnimateTo(1, new TweenSpec<double>(_converter, Duration, Easings.FastOutSlowIn));
        _progress.Tick(delta);
        var progress = _progress.Value;
        TransformComposition.Begin(_outgoingPresenter);
        TransformComposition.Begin(_currentPresenter);
        TransformComposition.Begin(this);
        Transform.Exit.Apply(_outgoingPresenter, 1 - progress);
        Transform.Enter.Apply(_currentPresenter, progress);
        Transform.SizeTransform?.Apply(this, progress);
        if (_progress.IsRunning) return true;
        FinishTransition();
        return false;
    }

    private void FinishTransition()
    {
        TransformComposition.End(_outgoingPresenter);
        TransformComposition.End(_currentPresenter);
        TransformComposition.End(this);
        ClearOutgoing();
        _transitioning = false;
        TransitionCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void ClearOutgoing()
    {
        _outgoingPresenter.Content = null;
        _outgoingPresenter.IsVisible = false;
    }

    bool ITransitionTrack.IsRunning => IsTransitioning;
    bool ITransitionTrack.Tick(TimeSpan delta) => Tick(delta);
    void ITransitionTrack.Pause() => _progress.Pause();
    void ITransitionTrack.Resume() => _progress.Resume();
    void ITransitionTrack.Complete() => CompleteTransition();
    void ITransitionTrack.Cancel() => CancelTransition();
}
