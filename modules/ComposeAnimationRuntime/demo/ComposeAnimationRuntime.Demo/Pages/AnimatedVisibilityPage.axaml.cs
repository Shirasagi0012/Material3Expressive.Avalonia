using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class AnimatedVisibilityPage : MotionPageBase
{
    public AnimatedVisibilityPage() => InitializeComponent();
    protected override void OnAttached()
    {
        Visibility.Enter = EnterTransition.FadeIn().TogetherWith(EnterTransition.ScaleIn());
        Visibility.Exit = ExitTransition.FadeOut().TogetherWith(ExitTransition.ScaleOut());
    }
    private void OnShow(object? sender, RoutedEventArgs e) { Visibility.IsShown = true; Status.Text = "shown"; }
    private void OnHide(object? sender, RoutedEventArgs e) { Visibility.IsShown = false; Status.Text = "hidden"; }
}
