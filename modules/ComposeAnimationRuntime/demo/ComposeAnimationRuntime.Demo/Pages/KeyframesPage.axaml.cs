using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class KeyframesPage : MotionPageBase
{
    private readonly DoubleVectorConverter _converter = new();
    private readonly Animatable<double> _position = new(0, new DoubleVectorConverter());

    public KeyframesPage() => InitializeComponent();

    protected override void OnAttached() => Bind(Box, Canvas.LeftProperty, _position);

    private void OnAnimate(object? sender, RoutedEventArgs e)
    {
        _position.SnapTo(0);
        _position.AnimateTo(560, new KeyframesSpec<double>(_converter, TimeSpan.FromSeconds(1),
            new(TimeSpan.Zero, 0),
            new(TimeSpan.FromMilliseconds(300), 420, Easings.EaseOut),
            new(TimeSpan.FromMilliseconds(700), 160, Easings.EaseIn),
            new(TimeSpan.FromSeconds(1), 560)));
        Status.Text = "4 keyframes · per-segment easing";
        RequestFrame();
    }
}
