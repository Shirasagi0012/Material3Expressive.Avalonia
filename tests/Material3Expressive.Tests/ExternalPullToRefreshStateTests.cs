using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class ExternalPullToRefreshStateTests
{
    [AvaloniaTheory]
    [InlineData("box")] [InlineData("circular")] [InlineData("loading")]
    public async Task ObservableExternalStateUpdatesTemplatesAndUnsubscribesOnReplacementAndDetach(string kind)
    {
        using var clock = new MotionTestClockScope();
        var first = new ExternalState(.5f); var second = new ExternalState(1.5f);
        PullToRefreshIndicatorBox indicator = kind switch {
            "circular" => new PullToRefreshIndicator(), "loading" => new PullToRefreshLoadingIndicator(),
            _ => new PullToRefreshIndicatorBox { Content = new Border { Width = 16, Height = 16 } }
        };
        indicator.State = first; var window = Show(indicator);
        try
        {
            var layout = indicator.GetVisualDescendants().OfType<PullToRefreshIndicatorLayout>().Single();
            var surface = indicator.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(.5 * indicator.MaxDistance - surface.Bounds.Height, layout.TranslationY);
            await first.SnapToAsync(1.25f); Flush(window);
            Assert.Equal(1.25 * indicator.MaxDistance - surface.Bounds.Height, layout.TranslationY);
            if (kind == "circular") Assert.Equal(1.25f, indicator.GetVisualDescendants().OfType<PullToRefreshArrow>().Single().DistanceFraction);
            if (kind == "loading") Assert.Equal(1.25, indicator.GetVisualDescendants().OfType<ContainedLoadingIndicator>().Single().Progress);
            indicator.State = second; Flush(window);
            Assert.Equal(0, first.Subscribers);
            Assert.Equal(1.5 * indicator.MaxDistance - surface.Bounds.Height, layout.TranslationY);
            await first.SnapToAsync(0); Flush(window);
            Assert.Equal(1.5 * indicator.MaxDistance - surface.Bounds.Height, layout.TranslationY);
            second.SetDistance(.75f, allProperties: true); Flush(window);
            Assert.Equal(.75 * indicator.MaxDistance - surface.Bounds.Height, layout.TranslationY);
            window.Content = null; Flush(window);
            Assert.Equal(0, second.Subscribers);
            await second.SnapToAsync(2); window.Content = indicator; Flush(window);
            Assert.Equal(2 * indicator.MaxDistance - surface.Bounds.Height, layout.TranslationY);
            indicator.State = null; Flush(window);
            Assert.IsType<PullToRefreshState>(indicator.EffectiveState);
            Assert.Equal(0, second.Subscribers);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public async Task ScrollBehaviorDispatchesExternalOperationsAndHonorsItsAnimatingGate()
    {
        using var clock = new MotionTestClockScope();
        var state = new ExternalState(2); var behavior = new PullToRefreshScrollBehavior();
        var indicator = new PullToRefreshIndicator { State = state, ScrollBehavior = behavior };
        var window = Show(indicator); clock.SendFrame(TimeSpan.Zero); Flush(window);
        try
        {
            Assert.Equal(0, state.DistanceFraction); // Attach initializes through the interface.
            state.IsAnimating = true;
            Assert.Equal(0, behavior.OnPostScroll(80)); Assert.Equal(0, behavior.OnPreScroll(-40));
            state.IsAnimating = false;
            Assert.Equal(240, behavior.OnPostScroll(240)); Flush(window);
            Assert.True(state.DistanceFraction > 1);
            var requested = 0; behavior.RefreshRequested += (_, _) => requested++;
            Assert.Equal(200, await behavior.OnPreFlingAsync(200));
            Assert.Equal(1, requested); Assert.Equal(1, state.HiddenCalls);
            Assert.Equal(0, state.DistanceFraction);
            indicator.IsRefreshing = true;
            clock.SendFrame(TimeSpan.FromMilliseconds(16)); Flush(window);
            Assert.Equal(1, state.ThresholdCalls); Assert.Equal(1, state.DistanceFraction);
            indicator.IsRefreshing = false;
            clock.SendFrame(TimeSpan.FromMilliseconds(32)); Flush(window);
            Assert.Equal(2, state.HiddenCalls); Assert.Equal(0, state.DistanceFraction);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task DetachedBehaviorCancelsCallerOwnedExternalOperation()
    {
        using var clock = new MotionTestClockScope();
        var state = new ExternalState(0) { WaitOnHide = true };
        var behavior = new PullToRefreshScrollBehavior();
        var indicator = new PullToRefreshIndicator { State = state, ScrollBehavior = behavior };
        var window = Show(indicator); clock.SendFrame(TimeSpan.Zero); Flush(window);
        behavior.OnPostScroll(80); Flush(window);
        var pending = behavior.OnPreFlingAsync(200);
        Assert.False(pending.IsCompleted);
        window.Content = null; Flush(window);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.True(state.LastToken.IsCancellationRequested);
        Assert.Equal(0, state.Subscribers); window.Close();
    }

    private sealed class ExternalState(float initial) : IPullToRefreshState
    {
        private PropertyChangedEventHandler? _changed;
        public int Subscribers { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { _changed += value; Subscribers++; }
            remove { _changed -= value; Subscribers--; }
        }
        public float DistanceFraction { get; private set; } = initial;
        public bool IsAnimating { get; set; }
        public int HiddenCalls { get; private set; }
        public int ThresholdCalls { get; private set; }
        public bool WaitOnHide { get; init; }
        public CancellationToken LastToken { get; private set; }
        public void SetDistance(float value, bool allProperties = false)
        { DistanceFraction = value; _changed?.Invoke(this, new(allProperties ? null : nameof(DistanceFraction))); }
        public Task SnapToAsync(float distanceFraction, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); SetDistance(distanceFraction); return Task.CompletedTask; }
        public Task AnimateToThresholdAsync(CancellationToken cancellationToken = default)
        { ThresholdCalls++; return SnapToAsync(1, cancellationToken); }
        public Task AnimateToHiddenAsync(CancellationToken cancellationToken = default)
        {
            HiddenCalls++; LastToken = cancellationToken;
            return WaitOnHide ? Task.Delay(Timeout.Infinite, cancellationToken) : SnapToAsync(0, cancellationToken);
        }
    }
    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 300, Height = 300 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme());
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
