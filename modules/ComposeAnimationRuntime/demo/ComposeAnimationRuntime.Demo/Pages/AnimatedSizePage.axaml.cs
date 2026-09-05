using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class AnimatedSizePage : MotionPageBase
{
    private bool _large;
    public AnimatedSizePage() => InitializeComponent();
    private void OnToggle(object? sender, RoutedEventArgs e) { _large = !_large; Card.Width = _large ? 340 : 180; Card.Height = _large ? 96 : 56; Status.Text = _large ? "large" : "compact"; }
}
