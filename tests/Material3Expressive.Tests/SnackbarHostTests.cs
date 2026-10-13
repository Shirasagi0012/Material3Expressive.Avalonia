using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class SnackbarHostTests
{
    [AvaloniaFact]
    public async Task KeepsOutgoingDataUntilOpacityFinishes_WhileNextRequestEnters()
    {
        using var clock = new MotionTestClockScope();
        var host = Host();
        var first = host.State.ShowSnackbarAsync("first", duration: SnackbarDuration.Indefinite, cancellationToken: TestContext.Current.CancellationToken);
        var second = host.State.ShowSnackbarAsync("second", duration: SnackbarDuration.Indefinite, cancellationToken: TestContext.Current.CancellationToken);
        var window = DialogTests.Show(host);
        Advance(clock, window, 30);
        var old = Layers(host).Single();
        Assert.Equal(1, old.Opacity);
        host.State.CurrentSnackbarData!.PerformAction();
        Assert.Equal(SnackbarResult.ActionPerformed, await first);
        Flush(window);
        Assert.Single(Layers(host)); // State changed; presentation commits on the next frame.
        Advance(clock, window, 1);
        Assert.Equal(2, Layers(host).Length);
        Assert.Contains(Layers(host), p => ReferenceEquals(p, old));
        Assert.Equal(AutomationLiveSetting.Off, AutomationProperties.GetLiveSetting(old));
        var current = Layers(host).Single(p => !ReferenceEquals(p, old));
        Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(current));
        Advance(clock, window, 4);
        Assert.InRange(old.Opacity, 0.01, .99);
        Advance(clock, window, 30);
        Assert.Same(current, Layers(host).Single());
        host.State.CurrentSnackbarData!.Dismiss(); await second;
        Assert.Single(Layers(host));
        Advance(clock, window, 30);
        Assert.Empty(Layers(host));
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaFact]
    public async Task DetachCancelsHostWork_PreservesApplicationRequest_AndReattachResumesDisplay()
    {
        using var clock = new MotionTestClockScope();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var host = Host();
        var showing = host.State.ShowSnackbarAsync("message", duration: SnackbarDuration.Indefinite, cancellationToken: cancellation.Token);
        var data = host.State.CurrentSnackbarData;
        var root = new Grid { Children = { host } };
        var window = DialogTests.Show(root);
        Advance(clock, window, 3);
        root.Children.Remove(host); Flush(window);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Same(data, host.State.CurrentSnackbarData);
        Assert.False(showing.IsCompleted);
        root.Children.Add(host); Flush(window); Advance(clock, window, 30);
        Assert.Single(Layers(host));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => showing);
        Advance(clock, window, 30);
        Assert.Empty(Layers(host));
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaFact]
    public async Task StateAndTemplateReplacement_KeepOldExitAndUseNewData_AndAutomationDismisses()
    {
        using var clock = new MotionTestClockScope();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var host = Host();
        var oldState = host.State;
        var first = oldState.ShowSnackbarAsync("old", duration: SnackbarDuration.Indefinite, cancellationToken: cancellation.Token);
        var window = DialogTests.Show(host);
        Advance(clock, window, 30);
        var next = new SnackbarHostState();
        var second = next.ShowSnackbarAsync("new", duration: SnackbarDuration.Indefinite, cancellationToken: TestContext.Current.CancellationToken);
        host.State = next; Flush(window);
        Assert.Single(Layers(host));
        Advance(clock, window, 1);
        Assert.Equal(2, Layers(host).Length);
        Assert.False(first.IsCompleted);
        host.SnackbarTemplate = new FuncDataTemplate<ISnackbarData>((_, _) => new Border { Width = 96, Height = 24, Background = Brushes.Blue });
        Flush(window); Advance(clock, window, 30);
        var layer = Layers(host).Single();
        Assert.Equal(96, layer.Bounds.Width);
        var peer = ControlAutomationPeer.CreatePeerForElement(layer)!;
        Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
        Assert.Equal(AutomationLiveSetting.Polite, peer.GetLiveSetting());
        Assert.IsAssignableFrom<IInvokeProvider>(peer).Invoke();
        Assert.Equal(SnackbarResult.Dismissed, await second);
        cancellation.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Advance(clock, window, 30);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(SnackbarDuration.Short, 4000)]
    [InlineData(SnackbarDuration.Long, 10000)]
    public async Task TimeoutUsesDesktopDuration_AndDoesNotDependOnAnimationFrames(SnackbarDuration duration, int milliseconds)
    {
        using var clock = new MotionTestClockScope();
        var host = Host();
        var task = host.State.ShowSnackbarAsync("timed", "action", duration: duration, cancellationToken: TestContext.Current.CancellationToken);
        var start = System.Diagnostics.Stopwatch.StartNew();
        var window = DialogTests.Show(host);
        Assert.Equal(SnackbarResult.Dismissed, await task.WaitAsync(TimeSpan.FromMilliseconds(milliseconds + 3000), TestContext.Current.CancellationToken));
        Assert.True(start.Elapsed.TotalMilliseconds >= milliseconds - 100);
        Assert.Single(Layers(host)); // Exit still needs animation frames.
        Advance(clock, window, 30);
        Assert.Empty(Layers(host));
    }

    private static SnackbarHost Host() => new()
    {
        SnackbarTemplate = new FuncDataTemplate<ISnackbarData>((_, _) => new Border { Width = 80, Height = 24, Background = Brushes.Red })
    };
    private static SnackbarPresenter[] Layers(SnackbarHost host) => host.GetVisualDescendants().OfType<SnackbarPresenter>().ToArray();
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    {
        for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
    }
}
