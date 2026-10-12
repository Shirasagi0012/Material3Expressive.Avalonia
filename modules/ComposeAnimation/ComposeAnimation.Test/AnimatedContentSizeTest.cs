using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimatedContentSizeTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task RepeatedChanges_AfterIdle_AnimateAndMoveSibling(bool replaceContent)
        => HeadlessApp.Session.Dispatch(() =>
        {
            var clock = new ManualFrameSource();
            var content = new Border { Width = 100, Height = 40 };
            var host = new AnimatedContentSize
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Left,
                AnimationSpec = new TweenSpec<Size>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing())
            };
            var completions = new List<AnimatedContentSizeChangedEventArgs>();
            host.AnimationCompleted += (_, e) => completions.Add(e);

            var sibling = new Border { Height = 20 };
            var panel = new StackPanel { Children = { host, sibling } };
            var window = new AnimationTestWindow(clock) { Width = 500, Height = 500, Content = panel };
            void Layout() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
            void Step(int ms) { clock.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
            window.Show();
            try
            {
                Layout();
                Assert.Equal(40, host.Bounds.Height);
                Assert.Equal(0, clock.PendingFrameCount);
                var previous = 40d;
                foreach (var target in new[] { 140d, 60d, 180d })
                {
                    Step(10000); // A real user pauses between clicks.
                    if (replaceContent)
                        host.Content = content = new Border { Width = 100, Height = target };
                    else content.Height = target;
                    Layout();
                    Step(16);
                    Assert.Equal(previous, host.Bounds.Height);
                    Step(50);
                    Assert.Equal((previous + target) / 2, host.Bounds.Height, 5);
                    Assert.Equal(host.Bounds.Bottom, sibling.Bounds.Y, 5);
                    Step(50);
                    Assert.Equal(target, host.Bounds.Height);
                    Assert.Equal(target, content.Bounds.Height);
                    var completed = completions.Last();
                    Assert.Equal(previous, completed.InitialSize.Height);
                    Assert.Equal(target, completed.TargetSize.Height);
                    Assert.True(host.GetVisualDescendants().OfType<ContentPresenter>().Single().IsArrangeValid);
                    previous = target;
                }
                Assert.Equal(3, completions.Count);
                Assert.Equal(0, clock.PendingFrameCount);
            }
            finally { window.Close(); }
        }, CancellationToken.None);
}
