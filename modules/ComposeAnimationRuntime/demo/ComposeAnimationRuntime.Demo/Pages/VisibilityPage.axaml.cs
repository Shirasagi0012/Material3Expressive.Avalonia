using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class VisibilityPage : MotionPageBase
{
    private readonly DoubleVectorConverter _converter = new();
    private readonly Animatable<double> _opacity = new(1, new DoubleVectorConverter());

    public VisibilityPage() => InitializeComponent();

    protected override void OnAttached() => Bind(Card, Visual.OpacityProperty, _opacity);

    private void OnShow(object? sender, RoutedEventArgs e)
    {
        _opacity.AnimateTo(1, new SpringSpec<double>(_converter));
        Status.Text = "showing";
        RequestFrame();
    }

    private void OnHide(object? sender, RoutedEventArgs e)
    {
        _opacity.AnimateTo(0, new TweenSpec<double>(_converter, TimeSpan.FromMilliseconds(180)));
        Status.Text = "hiding";
        RequestFrame();
    }
}
