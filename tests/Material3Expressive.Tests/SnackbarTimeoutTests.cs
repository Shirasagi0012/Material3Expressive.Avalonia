using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class SnackbarTimeoutTests
{
    [AvaloniaTheory]
    [InlineData(SnackbarDuration.Short, 4000, 0)]
    [InlineData(SnackbarDuration.Short, 4000, 2)]
    [InlineData(SnackbarDuration.Long, 10000, 1)]
    public async Task OrdinaryTimeDismissesAtDeadline_WithoutAnimationFramesOrDurationScaling(
        SnackbarDuration duration, int milliseconds, double durationScale)
    {
        using var fixture = new Host();
        fixture.Window.AnimationRoot.DurationScale = (float)durationScale;
        var request = fixture.Snackbar.State.ShowSnackbarAsync("timed", "action", duration: duration,
            cancellationToken: TestContext.Current.CancellationToken);
        fixture.Frame(); // Commit the LaunchedEffect equivalent at ordinary time zero.
        fixture.Advance(milliseconds - 1);
        Assert.False(request.IsCompleted);
        fixture.Advance(1);
        Assert.True(request.IsCompletedSuccessfully);
        Assert.Equal(SnackbarResult.Dismissed, await request);
        Assert.Null(fixture.Snackbar.State.CurrentSnackbarData);
        Assert.Single(fixture.Snackbar.GetVisualDescendants().OfType<SnackbarPresenter>());
        Assert.Equal(0, fixture.Time.ActiveTimerCount);
    }

    [AvaloniaFact]
    public async Task DetachCancelsDeadline_ReattachStartsFullDuration_WithoutCancellingRequest()
    {
        using var fixture = new Host();
        var request = fixture.Snackbar.State.ShowSnackbarAsync("timed", duration: SnackbarDuration.Short,
            cancellationToken: TestContext.Current.CancellationToken);
        fixture.Frame(); fixture.Advance(3000);
        fixture.Root.Children.Remove(fixture.Snackbar); fixture.Draw();
        Assert.Equal(0, fixture.Time.ActiveTimerCount);
        fixture.Advance(5000);
        Assert.False(request.IsCompleted);
        fixture.Root.Children.Add(fixture.Snackbar); fixture.Draw();
        fixture.Advance(3999);
        Assert.False(request.IsCompleted);
        fixture.Advance(1);
        Assert.Equal(SnackbarResult.Dismissed, await request);
    }

    [AvaloniaFact]
    public async Task ReplacingStateCancelsOldTimer_AndTemplateChangesDoNotRestartNewTimer()
    {
        using var fixture = new Host();
        using var cancellation = new CancellationTokenSource();
        var oldState = fixture.Snackbar.State;
        var oldRequest = oldState.ShowSnackbarAsync("old", duration: SnackbarDuration.Short,
            cancellationToken: cancellation.Token);
        fixture.Frame(); fixture.Advance(3000);
        fixture.Snackbar.State = new();
        var nextRequest = fixture.Snackbar.State.ShowSnackbarAsync("next", duration: SnackbarDuration.Short,
            cancellationToken: TestContext.Current.CancellationToken);
        fixture.Frame();
        fixture.Advance(1000); // The removed state's old deadline.
        Assert.False(oldRequest.IsCompleted); Assert.False(nextRequest.IsCompleted);
        fixture.Snackbar.SnackbarTemplate = Template(96); fixture.Frame();
        fixture.Advance(2999); Assert.False(nextRequest.IsCompleted);
        fixture.Advance(1);
        Assert.Equal(SnackbarResult.Dismissed, await nextRequest);
        Assert.False(oldRequest.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => oldRequest);
    }

    [AvaloniaFact]
    public async Task QueueReceivesItsOwnDeadline_AndIndefiniteDoesNotExpire()
    {
        using var fixture = new Host();
        using var cancellation = new CancellationTokenSource();
        var first = fixture.Snackbar.State.ShowSnackbarAsync("first", duration: SnackbarDuration.Short,
            cancellationToken: cancellation.Token);
        var second = fixture.Snackbar.State.ShowSnackbarAsync("second", duration: SnackbarDuration.Long,
            cancellationToken: cancellation.Token);
        var third = fixture.Snackbar.State.ShowSnackbarAsync("third", duration: SnackbarDuration.Indefinite,
            cancellationToken: cancellation.Token);
        fixture.Frame(); fixture.Advance(4000);
        Assert.Equal(SnackbarResult.Dismissed, await first);
        Assert.Equal("second", fixture.Snackbar.State.CurrentSnackbarData?.Visuals.Message);
        fixture.Frame(); fixture.Advance(9999); Assert.False(second.IsCompleted);
        fixture.Advance(1); Assert.Equal(SnackbarResult.Dismissed, await second);
        fixture.Frame(); fixture.Advance(1000000); Assert.False(third.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => third);
        fixture.Frame();
        Assert.Equal(0, fixture.Time.ActiveTimerCount);
    }

    private static IDataTemplate Template(double width) => new FuncDataTemplate<ISnackbarData>((_, _) =>
        new Border { Width = width, Height = 24, Background = Brushes.Red });

    private sealed class Host : IDisposable
    {
        public ManualTimeProvider Time { get; } = new(Dispatcher.UIThread.VerifyAccess);
        private ManualFrameSource Clock { get; } = new(Dispatcher.UIThread.VerifyAccess);
        public SnackbarHost Snackbar { get; } = new() { SnackbarTemplate = Template(80) };
        public Grid Root { get; } = new();
        public AnimationTestWindow Window { get; }
        private int _frame;
        public Host()
        {
            Window = new(Clock, Time) { Width = 240, Height = 160 };
            Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
            Root.Children.Add(Snackbar); Window.Content = Root; Window.Show(); Draw();
        }
        public void Draw() { Window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
        public void Frame() { Clock.SendFrame(TimeSpan.FromMilliseconds(++_frame * 16)); Draw(); }
        public void Advance(int milliseconds) { Time.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds)); Draw(); }
        public void Dispose()
        {
            Window.Close(); Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, Time.ActiveTimerCount); Assert.Equal(0, Clock.PendingFrameCount);
            Assert.Null(Window.AnimationRoot.Diagnostics.LastError); Clock.Dispose();
        }
    }
}
