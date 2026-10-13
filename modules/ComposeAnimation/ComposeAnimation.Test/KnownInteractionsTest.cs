using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

/// <summary>O6 characterization: Avalonia Transitions and Animated.Properties on one property.</summary>
public sealed class KnownInteractionsTest
{
    [Fact]
    public Task AvaloniaTransition_AndAnimatedProperties_OnTheSameProperty_LastOverlayWinsThenTransitionShows() => OnUi(() =>
    {
        var frames = new ManualFrameSource();
        var border = new Border
        {
            Width = 40, Height = 40, Background = Brushes.Red,
            Transitions = [new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromMilliseconds(160) }],
        };
        Animated.SetProperties(border, new AnimatedPropertyCollection
        {
            new AnimatedDouble { Property = Visual.OpacityProperty, Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(160), easing: new LinearEasing()) }
        });
        var window = new AnimationTestWindow(frames) { Width = 200, Height = 200, Content = border };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        border.Opacity = 0;
        var samples = new List<double>();
        Dispatcher.UIThread.RunJobs();
        for (var i = 0; i < 12; i++)
        {
            frames.AdvanceBy(TimeSpan.FromMilliseconds(16)); Dispatcher.UIThread.RunJobs();
            samples.Add(border.Opacity);
        }
        // While its run is live, the Animated.Properties overlay (the newer Animation-priority
        // contribution) wins: a plain 1 -> 0 tween.
        for (var i = 0; i < 10; i++) Assert.Equal(1 - i * 0.1, samples[i], 6);
        // When it ends, the Avalonia transition's own Animation-priority value shows again. Its
        // clock is Avalonia's, not the scope's FrameClock, and does not tick here, so it is still 1.
        Assert.Equal(1, samples[10]);
        Assert.Equal(1, samples[11]);
        Assert.Equal(0, border.GetBaseValue(Visual.OpacityProperty).Value);
        window.Close();
    });
}
