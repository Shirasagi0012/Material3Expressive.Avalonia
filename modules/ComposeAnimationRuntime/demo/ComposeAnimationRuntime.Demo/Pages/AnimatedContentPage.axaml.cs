using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class AnimatedContentPage : MotionPageBase
{
    private bool _state;
    public AnimatedContentPage() => InitializeComponent();
    protected override void OnAttached()
    {
        ContentAnimator.ContentFactory = state => CreateCard((bool?)state == true ? "State B" : "State A", (bool?)state == true ? "#E87532" : "#4F7CFF");
        ContentAnimator.KeySelector = state => state;
        ContentAnimator.Transform = new ContentTransform(EnterTransition.FadeIn().TogetherWith(EnterTransition.ScaleIn()), ExitTransition.FadeOut());
        ContentAnimator.TargetState = false;
    }
    private void OnSwitch(object? sender, RoutedEventArgs e) { _state = !_state; ContentAnimator.TargetState = _state; Status.Text = $"state = {(_state ? "B" : "A")}"; }
    private static Control CreateCard(string text, string color) => new Border { Width = 320, Height = 72, Background = Brush.Parse(color), CornerRadius = new CornerRadius(14), Padding = new Thickness(18), Child = new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 20, VerticalAlignment = VerticalAlignment.Center } };
}
