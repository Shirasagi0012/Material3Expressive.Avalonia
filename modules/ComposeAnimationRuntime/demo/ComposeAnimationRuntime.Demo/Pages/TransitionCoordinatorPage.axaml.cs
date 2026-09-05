using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimationRuntime;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class TransitionCoordinatorPage : MotionPageBase
{
    private readonly DoubleVectorConverter _converter = new();
    private readonly Animatable<double> _position = new(0, new DoubleVectorConverter());
    private readonly Animatable<double> _opacity = new(1, new DoubleVectorConverter());
    private TransitionCoordinator _coordinator;

    public TransitionCoordinatorPage()
    {
        InitializeComponent();
        _coordinator = new TransitionCoordinator();
        _coordinator.Add(_position);
        _coordinator.Add(_opacity);
    }

    protected override void OnAttached()
    {
        BindValue(_position, value => Canvas.SetLeft(PositionBox, value));
        BindValue(_opacity, value => OpacityBox.Opacity = value);
        RegisterTrack(delta => _coordinator.Tick(delta), () => _coordinator.IsRunning);
    }

    private void OnStart(object? sender, RoutedEventArgs e)
    {
        if (_coordinator.State is not TransitionSessionState.Idle)
        {
            _coordinator = new TransitionCoordinator();
            _coordinator.Add(_position);
            _coordinator.Add(_opacity);
        }
        _position.SnapTo(0);
        _opacity.SnapTo(1);
        _position.AnimateTo(520, new TweenSpec<double>(_converter, TimeSpan.FromMilliseconds(900), Easings.FastOutSlowIn));
        _opacity.AnimateTo(0.15, new SpringSpec<double>(_converter, 0.8, 260));
        _coordinator.Start();
        Status.Text = "running";
        RequestFrame();
    }

    private void OnPause(object? sender, RoutedEventArgs e)
    {
        _coordinator.Pause();
        Status.Text = "paused";
    }

    private void OnResume(object? sender, RoutedEventArgs e)
    {
        _coordinator.Resume();
        Status.Text = "running";
        RequestFrame();
    }

    private void OnComplete(object? sender, RoutedEventArgs e)
    {
        _coordinator.Complete();
        Status.Text = "completed · values settled at their targets";
        RequestFrame();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        _coordinator.Cancel();
        _position.SnapTo(0);
        _opacity.SnapTo(1);
        Status.Text = "canceled · values reset to their starting state";
        RequestFrame();
    }
}
