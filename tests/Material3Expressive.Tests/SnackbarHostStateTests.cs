using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class SnackbarHostStateTests
{
    [Theory]
    [InlineData(null, SnackbarDuration.Short)]
    [InlineData("", SnackbarDuration.Indefinite)]
    [InlineData("Undo", SnackbarDuration.Indefinite)]
    public async Task DefaultsDependOnActionPresence(string? action, SnackbarDuration expected)
    {
        var state = new SnackbarHostState();
        var task = state.ShowSnackbarAsync("message", action, cancellationToken: TestContext.Current.CancellationToken);
        var data = Assert.IsAssignableFrom<ISnackbarData>(state.CurrentSnackbarData);
        Assert.Equal(expected, data.Visuals.Duration);
        Assert.False(data.Visuals.WithDismissAction);
        Assert.False(task.IsCompleted);
        data.Dismiss();
        Assert.Equal(SnackbarResult.Dismissed, await task);
        Assert.Null(state.CurrentSnackbarData);
    }

    [Fact]
    public async Task FairQueuePreservesOrder_AndEqualVisualsRemainDistinctRequests()
    {
        var state = new SnackbarHostState();
        var displayed = new List<string>();
        state.PropertyChanged += (_, _) => { if (state.CurrentSnackbarData is { } data) displayed.Add(data.Visuals.Message); };
        var tasks = Enumerable.Range(0, 32).Select(i => state.ShowSnackbarAsync(i.ToString(), cancellationToken: TestContext.Current.CancellationToken)).ToArray();
        for (var i = 0; i < tasks.Length; i++)
        {
            Assert.Equal(i.ToString(), state.CurrentSnackbarData?.Visuals.Message);
            state.CurrentSnackbarData!.Dismiss();
            Assert.Equal(SnackbarResult.Dismissed, await tasks[i]);
        }
        Assert.Equal(Enumerable.Range(0, 32).Select(i => i.ToString()), displayed);
        var a = state.ShowSnackbarAsync("same", cancellationToken: TestContext.Current.CancellationToken);
        var first = state.CurrentSnackbarData!;
        var b = state.ShowSnackbarAsync("same", cancellationToken: TestContext.Current.CancellationToken);
        first.PerformAction();
        Assert.Equal(SnackbarResult.ActionPerformed, await a);
        Assert.NotSame(first, state.CurrentSnackbarData);
        first.Dismiss(); // A stale callback cannot complete the next request.
        Assert.False(b.IsCompleted);
        state.CurrentSnackbarData!.Dismiss();
        await b;
    }

    [Fact]
    public async Task CancelQueuedRequest_RemovesOnlyThatWaiter()
    {
        var state = new SnackbarHostState();
        var first = state.ShowSnackbarAsync("first", cancellationToken: TestContext.Current.CancellationToken);
        var data = state.CurrentSnackbarData;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var removed = state.ShowSnackbarAsync("removed", cancellationToken: cancellation.Token);
        var last = state.ShowSnackbarAsync("last", cancellationToken: TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => removed);
        Assert.Same(data, state.CurrentSnackbarData);
        Assert.False(first.IsCompleted);
        data!.Dismiss(); await first;
        Assert.Equal("last", state.CurrentSnackbarData?.Visuals.Message);
        state.CurrentSnackbarData!.PerformAction();
        Assert.Equal(SnackbarResult.ActionPerformed, await last);
    }

    [Fact]
    public async Task CancelCurrent_AdvancesQueue_AndStaleResultIsIgnored()
    {
        var state = new SnackbarHostState();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var first = state.ShowSnackbarAsync("first", cancellationToken: cancellation.Token);
        var stale = state.CurrentSnackbarData!;
        var second = state.ShowSnackbarAsync("second", cancellationToken: TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.Equal("second", state.CurrentSnackbarData?.Visuals.Message);
        stale.PerformAction(); stale.Dismiss();
        Assert.False(second.IsCompleted);
        state.CurrentSnackbarData!.Dismiss(); await second;
        Assert.Null(state.CurrentSnackbarData);
    }

    [Fact]
    public async Task ExplicitVisualsAndDurationArePreserved_AndCompletionIsIdempotent()
    {
        var state = new SnackbarHostState();
        var visuals = new CustomVisuals();
        var task = state.ShowSnackbarAsync(visuals, TestContext.Current.CancellationToken);
        var data = state.CurrentSnackbarData!;
        Assert.Same(visuals, data.Visuals);
        data.PerformAction(); data.PerformAction(); data.Dismiss();
        Assert.Equal(SnackbarResult.ActionPerformed, await task);
        var timed = state.ShowSnackbarAsync("explicit", "action", true, SnackbarDuration.Long, TestContext.Current.CancellationToken);
        Assert.Equal(SnackbarDuration.Long, state.CurrentSnackbarData!.Visuals.Duration);
        Assert.True(state.CurrentSnackbarData.Visuals.WithDismissAction);
        state.CurrentSnackbarData.Dismiss(); await timed;
    }

    [Fact]
    public async Task AlreadyCancelled_DoesNotPublishOrHoldTheQueue()
    {
        var state = new SnackbarHostState();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => state.ShowSnackbarAsync("cancelled", cancellationToken: new(true)));
        Assert.Null(state.CurrentSnackbarData);
        var next = state.ShowSnackbarAsync("next", cancellationToken: TestContext.Current.CancellationToken);
        state.CurrentSnackbarData!.Dismiss(); await next;
    }

    private sealed class CustomVisuals : ISnackbarVisuals
    {
        public string Message => "custom";
        public string? ActionLabel => "retry";
        public bool WithDismissAction => true;
        public SnackbarDuration Duration => SnackbarDuration.Indefinite;
        public int CustomValue => 42;
    }
}
