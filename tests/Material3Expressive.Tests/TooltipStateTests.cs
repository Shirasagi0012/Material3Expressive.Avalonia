using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class TooltipStateTests
{
    [Fact]
    public void DirectConstructorAndBoxDefault_PreserveDifferentPersistenceDefaults()
    {
        using var state = new TooltipState();
        Assert.True(state.IsPersistent);
        Assert.False(new TooltipBox().State.IsPersistent);
    }

    [Fact]
    public async Task PersistentDismiss_CancelsShowAndReleasesMutex()
    {
        using var state = new TooltipState(mutatorMutex: new());
        var show = state.ShowAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(state.IsVisible); Assert.False(show.IsCompleted);
        state.Dismiss();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => show);
        Assert.False(state.Transition.TargetState);
    }

    [Fact]
    public async Task EqualPriority_CancelsPreviousBeforePresentingNext()
    {
        var group = new MutatorMutex();
        using var first = new TooltipState(mutatorMutex: group);
        using var second = new TooltipState(mutatorMutex: group);
        var secondShown = new TaskCompletionSource();
        second.PropertyChanged += (_, _) => { if (second.Transition.TargetState) secondShown.TrySetResult(); };
        var a = first.ShowAsync(MutatePriority.UserInput, TestContext.Current.CancellationToken);
        var b = second.ShowAsync(MutatePriority.UserInput, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => a);
        await secondShown.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(first.Transition.TargetState); Assert.True(second.Transition.TargetState);
        second.Dismiss(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b);
        Assert.Null(group.CurrentCompletion);
    }

    [Fact]
    public async Task LowerPriority_IsRejected_AndIndependentGroupsRemainVisible()
    {
        var group = new MutatorMutex();
        using var first = new TooltipState(mutatorMutex: group);
        using var second = new TooltipState(mutatorMutex: group);
        using var independent = new TooltipState(mutatorMutex: new());
        var a = first.ShowAsync(MutatePriority.UserInput, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.ShowAsync(cancellationToken: TestContext.Current.CancellationToken));
        var c = independent.ShowAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(first.IsVisible); Assert.False(second.IsVisible); Assert.True(independent.IsVisible);
        first.Dismiss(); independent.Dismiss();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => a);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => c);
    }

    [Fact]
    public async Task NonpersistentUserInput_DismissesWithoutCompletingShow_UntilDispose()
    {
        using var state = new TooltipState(isPersistent: false, mutatorMutex: new());
        var show = state.ShowAsync(MutatePriority.UserInput, TestContext.Current.CancellationToken);
        state.Dismiss();
        Assert.False(state.IsVisible); Assert.False(show.IsCompleted);
        state.OnDispose(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => show);
    }

    [Fact]
    public async Task PreventUserInput_CancellationRetainsTarget_ForFocusAndLongPressOwner()
    {
        using var state = new TooltipState(isPersistent: false, mutatorMutex: new());
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var show = state.ShowAsync(MutatePriority.PreventUserInput, cancellation.Token);
        cancellation.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => show);
        Assert.True(state.Transition.TargetState);
        state.Dismiss(); Assert.False(state.IsVisible);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NonpersistentTimeout_PreservesDefaultVersusPreventUserInputCleanup(bool prevent)
    {
        using var state = new TooltipState(isPersistent: false, mutatorMutex: new());
        var show = state.ShowAsync(prevent ? MutatePriority.PreventUserInput : MutatePriority.Default, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => show.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(prevent, state.Transition.TargetState);
    }
}
