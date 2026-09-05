using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class AnimatedBoundsPage : MotionPageBase
{
    private bool _moved;
    public AnimatedBoundsPage() => InitializeComponent();
    private void OnMove(object? sender, RoutedEventArgs e) { _moved = !_moved; BoundsAnimator.TargetBounds = _moved ? new Rect(360, 120, 190, 88) : new Rect(24, 24, 150, 72); Status.Text = _moved ? "moved" : "origin"; }
}
