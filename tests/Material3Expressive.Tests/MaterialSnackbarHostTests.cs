// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// SnackbarHost.kt. The tests cover the Avalonia FIFO/cancellation adaptation and the host's
// pane/dismiss semantics; Compose's coroutine suspension is represented by Task results.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialSnackbarHostTests
{
    [Fact]
    public async Task QueueReturnsActionAndDismissResultsInOrder()
    {
        var state = new SnackbarHostState();
        var first = state.ShowSnackbarAsync(
            "Saved",
            "Undo",
            cancellationToken: TestContext.Current.CancellationToken);
        var second = state.ShowSnackbarAsync(
            "Completed",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Saved", state.CurrentSnackbarData!.Visuals.Message);
        state.CurrentSnackbarData.PerformAction();
        Assert.Equal(SnackbarResult.ActionPerformed, await first);

        Assert.Equal("Completed", state.CurrentSnackbarData!.Visuals.Message);
        state.CurrentSnackbarData.Dismiss();
        Assert.Equal(SnackbarResult.Dismissed, await second);
        Assert.Null(state.CurrentSnackbarData);
    }

    [Fact]
    public async Task CancellationRemovesQueuedAndCurrentRequests()
    {
        var state = new SnackbarHostState();
        using var currentCancellation = new CancellationTokenSource();
        using var queuedCancellation = new CancellationTokenSource();

        var current = state.ShowSnackbarAsync(
            "Current", cancellationToken: currentCancellation.Token);
        var queued = state.ShowSnackbarAsync(
            "Queued", cancellationToken: queuedCancellation.Token);
        queuedCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);

        currentCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => current);
        Assert.Null(state.CurrentSnackbarData);
    }

    [Fact]
    public void StateConstructorsAndVisualsPreserveRecoverableInitialValues()
    {
        var visuals = new SnackbarVisuals(
            "Needs attention",
            ActionLabel: "Review",
            WithDismissAction: true,
            Duration: SnackbarDuration.Indefinite);

        Assert.Equal("Needs attention", visuals.Message);
        Assert.Equal("Review", visuals.ActionLabel);
        Assert.True(visuals.WithDismissAction);
        Assert.Equal(SnackbarDuration.Indefinite, visuals.Duration);
    }

    [Fact]
    public void HostStateGetterIsAlwaysNonNullAndResettable()
    {
        var host = new MaterialSnackbarHost();
        var initial = host.State;

        host.ResetState();
        Assert.NotSame(initial, host.State);

        host.State = null!;
        Assert.NotNull(host.State);
        Assert.NotSame(initial, host.State);
    }

    [AvaloniaFact]
    public async Task HostCreatesSnackbarContentAndAutomationDismissesIt()
    {
        var state = new SnackbarHostState();
        var host = new MaterialSnackbarHost
        {
            State = state,
            Width = 320,
            Height = 80
        };
        ThemeTestHelper.ShowWindow(host);

        var pending = state.ShowSnackbarAsync("Archived");
        Assert.False(host.ClipToBounds);
        var snackbar = Assert.IsType<MaterialSnackbar>(host.CurrentSnackbar);
        Assert.True(snackbar.IsOpen);

        var peer = ControlAutomationPeer.CreatePeerForElement(host);
        Assert.Equal(AutomationLiveSetting.Polite, peer.GetLiveSetting());
        Assert.Equal("Archived", peer.GetName());
        var provider = Assert.IsAssignableFrom<IInvokeProvider>(
            peer.GetProvider<IInvokeProvider>());
        provider.Invoke();

        Assert.Equal(SnackbarResult.Dismissed, await pending);
        Assert.Null(host.CurrentSnackbar);
        Assert.Equal("Snackbar", peer.GetName());
    }

    [AvaloniaFact]
    public async Task GeneratedActionCompletesTheRequest()
    {
        var state = new SnackbarHostState();
        var host = new MaterialSnackbarHost { State = state };
        ThemeTestHelper.ShowWindow(host);

        var pending = state.ShowSnackbarAsync(
            "Saved",
            "Undo",
            cancellationToken: TestContext.Current.CancellationToken);
        var snackbar = Assert.IsType<MaterialSnackbar>(host.CurrentSnackbar);
        var action = Assert.IsType<Button>(snackbar.Action);
        var label = Assert.IsType<TextBlock>(action.Content);

        Assert.Equal(new Thickness(12), snackbar.Margin);
        Assert.Equal(new Thickness(16, 0, 8, 0), snackbar.Padding);
        Assert.Equal(new Thickness(12, 0), action.Padding);
        Assert.Equal(58, action.MinWidth);
        Assert.Equal(40, action.MinHeight);
        Assert.Equal(14, label.FontSize);
        Assert.Equal(20, label.LineHeight);
        Assert.Equal(0.1, label.LetterSpacing);
        Assert.Equal(FontWeight.Medium, label.FontWeight);
        Assert.Same(snackbar.ActionBrush, label.Foreground);

        action.Command!.Execute(null);

        Assert.Equal(SnackbarResult.ActionPerformed, await pending);
        Assert.Null(host.CurrentSnackbar);
    }

    [AvaloniaFact]
    public async Task HostUsesFastSpringsAndRetainsOutgoingItemUntilExitSettles()
    {
        using var clock = new MotionTestClockScope();
        var state = new SnackbarHostState();
        var host = new MaterialSnackbarHost { State = state };
        var pending = state.ShowSnackbarAsync(
            "Archived",
            duration: SnackbarDuration.Indefinite,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, host.CurrentOpacity);
        Assert.Equal(.8, host.CurrentScale);
        Assert.Equal(1, host.TransitionItemCount);

        var expectedOpacity = new SpringAnimation(
            0,
            ResolvedMotionScheme.Default.Get(MotionRole.FastEffects));
        var expectedScale = new SpringAnimation(
            .8,
            ResolvedMotionScheme.Default.Get(MotionRole.FastSpatial));
        expectedOpacity.AnimateTo(1);
        expectedScale.AnimateTo(1);
        var elapsed = TimeSpan.FromMilliseconds(100);
        expectedOpacity.Tick(elapsed);
        expectedScale.Tick(elapsed);
        clock.AdvanceBy(elapsed);

        Assert.Equal(expectedOpacity.Value, host.CurrentOpacity, 12);
        Assert.Equal(expectedScale.Value, host.CurrentScale, 12);

        state.CurrentSnackbarData!.Dismiss();
        Assert.Null(host.CurrentSnackbar);
        Assert.Equal(1, host.TransitionItemCount);
        Assert.Equal(SnackbarResult.Dismissed, await pending);

        clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(0, host.TransitionItemCount);
        Assert.Null(host.Content);
    }

    [AvaloniaFact]
    public async Task ShortTimeoutUsesTheComponentClockAndStartsExitAtFourSeconds()
    {
        using var clock = new MotionTestClockScope();
        var state = new SnackbarHostState();
        var host = new MaterialSnackbarHost { State = state };
        var pending = state.ShowSnackbarAsync(
            "Saved",
            duration: SnackbarDuration.Short,
            cancellationToken: TestContext.Current.CancellationToken);

        clock.AdvanceBy(TimeSpan.FromMilliseconds(3999));
        Assert.NotNull(state.CurrentSnackbarData);

        clock.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.Null(state.CurrentSnackbarData);
        Assert.Null(host.CurrentSnackbar);
        Assert.Equal(1, host.TransitionItemCount);
        Assert.Equal(SnackbarResult.Dismissed, await pending);
    }

    [AvaloniaFact]
    public void QueueTransitionOverlapsOutgoingAndIncomingItems()
    {
        using var clock = new MotionTestClockScope();
        var state = new SnackbarHostState();
        var host = new MaterialSnackbarHost { State = state };
        _ = state.ShowSnackbarAsync("First", duration: SnackbarDuration.Indefinite);
        _ = state.ShowSnackbarAsync("Second", duration: SnackbarDuration.Indefinite);
        clock.SettleAll();

        state.CurrentSnackbarData!.Dismiss();

        Assert.Equal("Second", state.CurrentSnackbarData!.Visuals.Message);
        Assert.Equal(2, host.TransitionItemCount);
        Assert.Equal("Second", Assert.IsType<TextBlock>(host.CurrentSnackbar!.Message).Text);

        clock.SettleAll();
        Assert.Equal(1, host.TransitionItemCount);
    }
}
