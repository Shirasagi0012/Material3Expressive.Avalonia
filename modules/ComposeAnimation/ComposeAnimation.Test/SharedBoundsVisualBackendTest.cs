using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SharedBoundsVisualBackendTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    [Fact]
    public Task BoundaryScaleChild_AndRemeasureTarget_StayReadyThroughReverse() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var transition = new Transition<bool>(true);
        var fromChild = new SharedBounds
        {
            Key = "color", Content = new Border { Width = 120, Height = 80 },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var sourceNative = new Grid { Children = { fromChild } };
        var from = Remeasure("card", new LookaheadBoundary { Child = sourceNative });
        var fromRoot = new LookaheadRoot { Width = 160, Height = 150, Child = from };

        var targetColor = new Border { Width = 440, Height = 170 };
        var toChild = Remeasure("color", new LookaheadBoundary
        {
            Child = targetColor
        });
        var targetStack = new LookaheadStackPanel { Children = { toChild, new LookaheadTextBlock { Text = "Details" } } };
        var to = Remeasure("card", targetStack);
        var toRoot = new LookaheadRoot { Width = 440, Height = 380, Child = to };
        var source = Visibility(fromRoot, true);
        var target = Visibility(toRoot, false);
        Canvas.SetLeft(source, 20); Canvas.SetTop(source, 40);
        Canvas.SetLeft(target, 140); Canvas.SetTop(target, 70);
        var host = new SharedTransitionHost
        {
            Width = 720, Height = 520, Transition = transition,
            PreparationTimeout = Timeout.InfiniteTimeSpan,
            Content = new Canvas { Children = { source, target } }
        };
        var window = new AnimationTestWindow(clock) { Width = 720, Height = 520, Content = host };
        try
        {
            window.Show(); Layout(); transition.OnFrame(TimeSpan.Zero);
            transition.UpdateTarget(false); Layout();
            var color = Assert.Single(host.Coordinator!.Motions, x => Equals(x.Pair.Target.Key, "color"));
            var sourceRect = color.Pair.SourceBounds;
            var targetRect = color.Pair.TargetBounds;
            transition.OnFrame(TimeSpan.Zero); transition.OnFrame(TimeSpan.FromMilliseconds(100)); Layout();
            Assert.All(host.Overlay!.Items, item => Assert.True(item.ReadyToDraw));

            transition.UpdateTarget(true); Layout();
            Assert.Equal(sourceRect, color.Pair.TargetBounds);
            transition.OnFrame(TimeSpan.FromMilliseconds(200)); Layout();
            Assert.Equal(sourceRect, color.Pair.TargetBounds);
            Assert.All(host.Overlay.Items, item => Assert.True(item.ReadyToDraw));
            transition.UpdateTarget(false); Layout();
            Assert.Equal(targetRect, color.Pair.TargetBounds);
            targetColor.Width = 480; toRoot.Width = 480; Layout();
            var updatedTarget = color.Pair.TargetBounds;
            Assert.Equal(480, updatedTarget.Width);
            transition.UpdateTarget(true); Layout();
            transition.UpdateTarget(false); Layout();
            Assert.Equal(updatedTarget, color.Pair.TargetBounds);
            transition.UpdateTarget(true); Layout();
            foreach (var milliseconds in new[] { 250, 300, 600, 1000, 5000 })
            {
                transition.OnFrame(TimeSpan.FromMilliseconds(milliseconds)); Layout();
                Assert.Equal(sourceRect, color.Pair.TargetBounds);
                Assert.All(host.Overlay.Items, item => Assert.True(item.ReadyToDraw));
            }
            Assert.False(host.IsOverlayActive);
            Assert.Equal(sourceRect, fromChild.ReadLayoutBounds());
            Assert.Null(host.LastSessionError);
        }
        finally
        {
            window.Close(); Dispatcher.UIThread.RunJobs(); transition.Dispose();
        }

        SharedBounds Remeasure(object key, Control content) => new()
        {
            Key = key, Content = content, ResizeMode = SharedBoundsResizeMode.RemeasureToBounds,
            BoundsTransform = new SharedBoundsTransform
            {
                Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(200), easing: new LinearEasing())
            }
        };
        AnimatedVisibility Visibility(Control content, bool state) => new()
        {
            Content = content, Source = VisibilitySource.From(transition, value => value == state),
            Enter = EnterTransition.None, Exit = ExitTransition.None
        };
        void Layout()
        {
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        }
    });
}
