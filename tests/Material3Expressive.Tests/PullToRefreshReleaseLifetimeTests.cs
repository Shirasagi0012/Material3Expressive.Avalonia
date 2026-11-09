using System.ComponentModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using SmoothScroll.Avalonia.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class PullToRefreshReleaseLifetimeTests
{
    [Theory]
    [InlineData(-100.1234567)]
    [InlineData(-250.7654321)]
    [InlineData(100.1234567)]
    public void FractionalConsumptionCannotExceedTheOriginalDoubleTransaction(double delta)
    {
        var participant = new PullToRefreshScrollParticipant(new(4, 320, false, true, false),
            distance: 2000, offset: 640, _ => { });
        var context = new ScrollMovementContext(delta, 0, 0, 0, 1000, 0, ScrollMovementSource.Direct);
        var consumed = delta > 0 ? participant.PreScroll(in context) : participant.PostScroll(in context, 0);
        Assert.InRange(consumed, Math.Min(0, delta), Math.Max(0, delta));
        Assert.Equal(delta, consumed, precision: 4);
        Assert.True(Math.Abs(consumed) > 0);
    }

    [AvaloniaTheory]
    [InlineData(-777.123456789)]
    [InlineData(-1500.987654321)]
    [InlineData(777.123456789)]
    public async Task FullVelocityConsumptionReturnsTheOriginalDoubleOffer(double offered)
    {
        using var clock = new MotionTestClockScope();
        var (window, box) = Show(new RecordingState());
        try
        {
            box.ScrollBehavior.OnPostScroll(200);
            Render(window);
            var connection = (PullToRefreshScrollConnection)typeof(PullToRefreshBox)
                .GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(box)!;
            Assert.NotNull(connection);
            var consumed = await connection.ConsumeReleaseAsync(offered, CancellationToken.None);
            Assert.Equal(offered < 0 ? offered : 0, consumed);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task RefreshCallbackCanReplaceStateWithoutSnappingOrAnimatingTheReplacement()
    {
        using var clock = new MotionTestClockScope();
        var original = new RecordingState();
        var replacement = new RecordingState(.625f);
        var (window, box) = Show(original);
        try
        {
            box.ScrollBehavior.OnPostScroll(200);
            Render(window);
            box.RefreshRequested += (_, _) => box.State = replacement;
            Assert.Equal(0, await box.ScrollBehavior.OnPreFlingAsync(333));
            Render(window);
            Assert.Equal(200, box.ScrollBehavior.DistancePulled);
            Assert.Equal(.625f, replacement.DistanceFraction);
            Assert.Equal(0, replacement.SnapCalls);
            Assert.Equal(0, replacement.HiddenCalls);
            Assert.Equal(0, original.HiddenCalls);
            box.ScrollBehavior.OnPostScroll(20);
            Render(window);
            Assert.Equal(220, box.ScrollBehavior.DistancePulled);
            Assert.True(replacement.DistanceFraction > 1);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task RefreshCallbackCanDetachTheBoxWithoutDereferencingTheOldOwner()
    {
        using var clock = new MotionTestClockScope();
        var state = new RecordingState();
        var (window, box) = Show(state);
        try
        {
            box.ScrollBehavior.OnPostScroll(200);
            Render(window);
            box.RefreshRequested += (_, _) => window.Content = null;
            Assert.Equal(0, await box.ScrollBehavior.OnPreFlingAsync(333));
            Render(window);
            Assert.Equal(0, state.HiddenCalls);
            Assert.False(box.IsScrollConnected);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task LateOldReleaseCannotResetTheReplacementStatesAccumulatedPull()
    {
        using var clock = new MotionTestClockScope();
        var original = new RecordingState { HiddenCompletion = new() };
        var replacement = new RecordingState(.625f);
        var (window, box) = Show(original);
        try
        {
            box.ScrollBehavior.OnPostScroll(200);
            Render(window);
            var release = box.ScrollBehavior.OnPreFlingAsync(333);
            Assert.False(release.IsCompleted);
            box.State = replacement;
            Render(window);
            Assert.True(original.HiddenToken.IsCancellationRequested);
            Assert.Equal(200, box.ScrollBehavior.DistancePulled);
            Assert.Equal(0, replacement.SnapCalls);
            Assert.Equal(.625f, replacement.DistanceFraction);
            box.ScrollBehavior.OnPostScroll(20);
            Render(window);
            var fraction = replacement.DistanceFraction;
            var resets = 0;
            box.ScrollBehavior.DistanceReset += () => resets++;
            original.HiddenCompletion.SetResult();
            await release;
            Render(window);
            Assert.Equal(0, resets);
            Assert.Equal(220, box.ScrollBehavior.DistancePulled);
            Assert.Equal(fraction, replacement.DistanceFraction);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { original.HiddenCompletion.TrySetResult(); window.Close(); }
    }

    [AvaloniaFact]
    public async Task LateReleaseFromBeforeDetachCannotResetTheReattachedHost()
    {
        using var clock = new MotionTestClockScope();
        var state = new RecordingState { HiddenCompletion = new() };
        var (window, box) = Show(state);
        try
        {
            box.ScrollBehavior.OnPostScroll(200);
            Render(window);
            var release = box.ScrollBehavior.OnPreFlingAsync(333);
            window.Content = null;
            Render(window);
            Assert.True(state.HiddenToken.IsCancellationRequested);
            window.Content = box;
            Render(window);
            box.ScrollBehavior.ApplyScrollSnapshot(40, 20, publish: true);
            Render(window);
            var resets = 0;
            box.ScrollBehavior.DistanceReset += () => resets++;
            state.HiddenCompletion.SetResult();
            await release;
            Render(window);
            Assert.Equal(40, box.ScrollBehavior.DistancePulled);
            Assert.Equal(20, box.ScrollBehavior.VerticalOffset);
            Assert.Equal(0, resets);
            Assert.True(box.IsScrollConnected);
        }
        finally { state.HiddenCompletion.TrySetResult(); window.Close(); }
    }

    [AvaloniaFact]
    public async Task ReusingTheSameStateDoesNotReviveItsPreviousRelease()
    {
        using var clock = new MotionTestClockScope();
        var state = new RecordingState { HiddenCompletion = new() };
        var (window, box) = Show(state);
        try
        {
            box.ScrollBehavior.OnPostScroll(200);
            Render(window);
            var release = box.ScrollBehavior.OnPreFlingAsync(333);
            box.State = new RecordingState();
            box.State = state;
            Render(window);
            Assert.True(state.HiddenToken.IsCancellationRequested);
            box.ScrollBehavior.OnPostScroll(20);
            Render(window);
            var fraction = state.DistanceFraction;
            var resets = 0;
            box.ScrollBehavior.DistanceReset += () => resets++;
            state.HiddenCompletion.SetResult();
            await release;
            Render(window);
            Assert.Equal(220, box.ScrollBehavior.DistancePulled);
            Assert.Equal(fraction, state.DistanceFraction);
            Assert.Equal(0, resets);
        }
        finally { state.HiddenCompletion.TrySetResult(); window.Close(); }
    }

    private sealed class RecordingState(float initial = 0) : IPullToRefreshState
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public float DistanceFraction { get; private set; } = initial;
        public bool IsAnimating => false;
        internal int SnapCalls, HiddenCalls;
        internal CancellationToken HiddenToken;
        internal TaskCompletionSource? HiddenCompletion;

        public Task AnimateToThresholdAsync(CancellationToken cancellationToken = default) => SnapToAsync(1, cancellationToken);
        public Task AnimateToHiddenAsync(CancellationToken cancellationToken = default)
        {
            HiddenCalls++;
            HiddenToken = cancellationToken;
            // Deliberately permit late completion to verify the host's identity barrier.
            return HiddenCompletion?.Task ?? SnapToAsync(0, cancellationToken);
        }
        public Task SnapToAsync(float distanceFraction, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SnapCalls++;
            DistanceFraction = distanceFraction;
            PropertyChanged?.Invoke(this, new(nameof(DistanceFraction)));
            return Task.CompletedTask;
        }
    }

    private static (MotionTestWindow Window, PullToRefreshBox Box) Show(IPullToRefreshState state)
    {
        var box = new PullToRefreshBox
        {
            State = state,
            Content = new ScrollViewer
            {
                Content = new Border { Height = 1200, Background = Brushes.LightBlue },
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                IsScrollInertiaEnabled = false,
            },
        };
        var window = new MotionTestWindow { Width = 240, Height = 320 };
        window.Styles.Add(new MaterialTheme());
        window.Content = box;
        window.Show();
        Render(window);
        Assert.True(box.IsScrollConnected);
        return (window, box);
    }

    private static void Render(Window window)
    {
        for (var turn = 0; turn < 4; turn++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            using (window.CaptureRenderedFrame()) { }
        }
        Dispatcher.UIThread.RunJobs();
    }
}
