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
        var snackbar = Assert.IsType<MaterialSnackbar>(host.Content);
        Assert.True(snackbar.IsOpen);

        var peer = ControlAutomationPeer.CreatePeerForElement(host);
        var provider = Assert.IsAssignableFrom<IInvokeProvider>(
            peer.GetProvider<IInvokeProvider>());
        provider.Invoke();

        Assert.Equal(SnackbarResult.Dismissed, await pending);
        Assert.Null(host.Content);
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
        var snackbar = Assert.IsType<MaterialSnackbar>(host.Content);
        var action = Assert.IsType<Button>(snackbar.Action);
        var label = Assert.IsType<TextBlock>(action.Content);

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
        Assert.Null(host.Content);
    }
}
