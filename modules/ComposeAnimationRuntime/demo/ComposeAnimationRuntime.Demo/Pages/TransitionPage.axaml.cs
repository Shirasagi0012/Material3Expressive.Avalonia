using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class TransitionPage : MotionPageBase
{
    private readonly DoubleVectorConverter _converter = new();
    private readonly Animatable<double> _offset = new(0, new DoubleVectorConverter());
    private readonly Animatable<double> _opacity = new(1, new DoubleVectorConverter());
    private readonly MotionTransition<bool> _transition = new(false);
    private bool _state;

    public TransitionPage()
    {
        InitializeComponent();
        _transition.Register("offset", _offset, value => value ? 480 : 0, _ => new SpringSpec<double>(_converter, 0.8, 320));
        _transition.Register("opacity", _opacity, value => value ? 0.35 : 1, _ => new TweenSpec<double>(_converter, TimeSpan.FromMilliseconds(240), Easings.FastOutSlowIn));
    }

    protected override void OnAttached()
    {
        BindProperty(Box, Canvas.LeftProperty, _offset);
        BindProperty(Box, Visual.OpacityProperty, _opacity);
        RegisterTrack(_transition.Tick, () => _transition.IsRunning);
    }

    private void OnToggle(object? sender, RoutedEventArgs e)
    {
        _state = !_state;
        _transition.AnimateTo(_state);
        Status.Text = $"state = {(_state ? "active" : "resting")}";
        RequestFrame();
    }
}
