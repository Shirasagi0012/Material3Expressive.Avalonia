using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class SpringPage : MotionPageBase
{
    private readonly DoubleVectorConverter _converter = new();
    private readonly Animatable<double> _position = new(0, new DoubleVectorConverter());

    public SpringPage() => InitializeComponent();

    protected override void OnAttached() => Bind(Box, Canvas.LeftProperty, _position);

    private void OnAnimate(object? sender, RoutedEventArgs e)
    {
        _position.AnimateTo(_position.Value > 100 ? 0 : 560, new SpringSpec<double>(_converter, 0.72, 260));
        Status.Text = $"target = {_position.TargetValue:0}";
        RequestFrame();
    }
}
