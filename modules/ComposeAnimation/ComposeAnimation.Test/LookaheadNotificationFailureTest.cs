using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadNotificationFailureTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task NotificationFailure_ClearsTargetAndCancelsEveryParticipant(bool legacyRoot)
        => HeadlessApp.Session.Dispatch(() =>
        {
            var clock = new ManualAnimationFrameClock();
            var first = Card();
            var second = Card();
            var box = new LookaheadStackPanel { Children = { first, second }, Width = 300, Height = 240 };
            var root = legacyRoot ? new LookaheadRoot { Child = box, Width = 300, Height = 240 } : null;
            Control entry = root is null ? box : root;
            var window = new AnimationTestWindow(clock) { Content = entry, Width = 400, Height = 340 };
            window.Show(); Layout();
            try
            {
                first.TargetSize = second.TargetSize = new Size(140, 60);
                Layout(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Layout();
                Assert.True(first.IsRunning);
                Assert.True(second.IsRunning);
                EventHandler legacy = (_, _) => throw new InvalidOperationException("notification failed");
                EventHandler<LookaheadTargetLayoutEventArgs> direct = (_, _) => throw new InvalidOperationException("notification failed");
                if (root is not null) root.TargetLayoutUpdated += legacy;
                else Lookahead.AddTargetLayoutUpdatedHandler(first, direct);
                first.TargetSize = new Size(100, 50);
                var error = Assert.Throws<InvalidOperationException>(() =>
                {
                    entry.Measure(new Size(300, 240)); entry.Arrange(new Rect(0, 0, 300, 240));
                });
                Assert.Equal("notification failed", error.Message);
                Assert.Null(Lookahead.GetSnapshot(entry));
                Assert.Null(first.TargetBounds);
                Assert.False(first.IsRunning);
                Assert.False(second.IsRunning);
                Assert.Equal(0, clock.PendingFrameCount);
                if (root is not null) root.TargetLayoutUpdated -= legacy;
                else Lookahead.RemoveTargetLayoutUpdatedHandler(first, direct);
                Layout();
                Assert.NotNull(Lookahead.GetSnapshot(entry));
                Assert.False(first.IsRunning);
                Assert.False(second.IsRunning);
            }
            finally { window.Close(); }

            AnimatedBounds Card()
            {
                var card = new AnimatedBounds
                {
                    Child = new LookaheadTextBlock { Text = "notification" }, TargetSize = new Size(60, 30),
                    BoundsTransform = new BoundsTransform { Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(200)) }
                };

                return card;
            }
            void Layout() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
        }, CancellationToken.None);
}
