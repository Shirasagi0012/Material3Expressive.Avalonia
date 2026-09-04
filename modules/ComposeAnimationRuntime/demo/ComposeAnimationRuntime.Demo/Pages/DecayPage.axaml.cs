using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class DecayPage : MotionPageBase
{
    private readonly DoubleVectorConverter _converter = new();
    private readonly Animatable<double> _position = new(0, new DoubleVectorConverter());

    public DecayPage() => InitializeComponent();

    protected override void OnAttached() => Bind(Box, Canvas.LeftProperty, _position);

    private void OnAnimate(object? sender, RoutedEventArgs e)
    {
        _position.SnapTo(0);
        _position.AnimateDecay(620, new DecaySpec<double>(_converter, 3.2));
        Status.Text = "initial velocity = 620 px/s";
        RequestFrame();
    }
}
