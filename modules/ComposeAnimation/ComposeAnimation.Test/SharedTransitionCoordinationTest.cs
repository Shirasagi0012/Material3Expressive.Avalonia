using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SharedTransitionCoordinationTest
{
    private static TimeSpan Ms(int ms) => TimeSpan.FromMilliseconds(ms);
    private static Transition<EnterExitState> Visibility(Transition<bool> root, bool source = true)
        => root.CreateChildTransition(s => s == source ? EnterExitState.Visible : EnterExitState.PostExit);

    [Theory]
    [InlineData("cancel")]
    [InlineData("fallback")]
    [InlineData("session")]
    [InlineData("failure")]
    [InlineData("dispose")]
    public void DetachedParticipant_IsReleasedWhenItsOwnerFinishes(string ending)
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var target = Visibility(root, false);
        using var scope = SharedTransitionScope.Create(root);
        using var from = scope.RegisterParticipant("photo", source);
        using var to = scope.RegisterParticipant("photo", target);
        root.UpdateTarget(false);
        from.Detach();
        Assert.True(from.IsActive);
        if (ending == "session")
        {
            SharedTransitionSession? session = null;
            Assert.True(scope.TryCommitSessions(scope.Generation, batch =>
                session = batch.Start(from, to, _ => { }, (_, _) => { })));
            Assert.True(from.IsActive);
            session!.Dispose();
        }
        else if (ending == "fallback")
            Assert.True(scope.TryCommitSessions(scope.Generation, _ => { }));
        else if (ending == "failure")
            Assert.Throws<InvalidOperationException>(() => scope.TryCommitSessions(scope.Generation,
                _ => throw new InvalidOperationException("installation failed")));
        else if (ending == "dispose") scope.Dispose();
        else Assert.True(scope.CancelPreparation(scope.Generation));
        Assert.False(from.IsActive);
        Assert.Equal(ending != "dispose", to.IsActive);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutomaticTargetChange_ReservesBeforeDescendantReconfiguration(bool directStateWrite)
    {
        var state = new MutableTransitionState<bool>(true);
        using var root = new Transition<bool>(state, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        SharedTransitionPreparation? request = null;
        scope.PreparationRequested += value => request = value;
        if (directStateWrite) state.TargetState = false;
        else root.UpdateTarget(false);
        Assert.NotNull(request);
        Assert.Equal(EnterExitState.Visible, request.Before.Single().TargetState);
        Assert.Equal(EnterExitState.PostExit, source.TargetState);
        Assert.True(source.IsPreparing);
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(60000));
        Assert.Equal(EnterExitState.Visible, source.CurrentState);
        Assert.False(state.IsIdle);
        Assert.True(scope.CancelPreparation(request.Generation));
        Assert.True(request.CancellationToken.IsCancellationRequested);
        root.OnFrame(Ms(60016));
        Assert.True(state.IsIdle);
    }

    [Fact]
    public void NestedUpdate_CoalescesTargetsAndSnapshotsBeforeFirstMutation()
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        var requests = new List<SharedTransitionPreparation>();
        scope.PreparationRequested += requests.Add;
        using (scope.BeginUpdate())
        {
            root.UpdateTarget(false);
            using (scope.BeginUpdate()) root.UpdateTarget(true);
            root.UpdateTarget(false);
            Assert.Empty(requests);
            Assert.True(scope.IsUpdating);
            Assert.Equal(1, scope.Generation);
        }
        var request = Assert.Single(requests);
        Assert.Equal(EnterExitState.Visible, request.Before.Single().TargetState);
        Assert.False(scope.IsUpdating);
        Assert.True(scope.IsPreparing);
        Assert.True(scope.CancelPreparation(request.Generation));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UpdateInsideParentNotification_OrdersPreparationTargetsAndCommit(bool cancel)
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        var requests = new List<SharedTransitionPreparation>();
        scope.PreparationRequested += requests.Add;
        var handled = false;
        root.PropertyChanged += (_, args) =>
        {
            if (handled || args.PropertyName != nameof(root.DurationScale)) return;
            handled = true;
            using (var update = scope.BeginUpdate())
            {
                root.UpdateTarget(false);
                root.UpdateTarget(true);
                root.UpdateTarget(false);
                if (cancel) update.Cancel();
            }
        };
        root.DurationScale = 1;
        Assert.False(scope.IsUpdating);
        Assert.Equal(EnterExitState.PostExit, source.TargetState);
        if (cancel)
        {
            Assert.Empty(requests);
            Assert.False(scope.IsPreparing);
            Assert.False(root.IsPreparing);
        }
        else
        {
            Assert.Single(requests);
            Assert.Equal(1, scope.Generation);
            Assert.True(source.IsPreparing);
            Assert.Equal(EnterExitState.Visible, requests[0].Before.Single().TargetState);
        }
    }

    [Fact]
    public void CancelNestedUpdate_DoesNotRollbackTarget_AndRevokesAllUpdateHandles()
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        var requests = 0;
        scope.PreparationRequested += _ => requests++;
        var outer = scope.BeginUpdate();
        var inner = scope.BeginUpdate();
        root.UpdateTarget(false);
        Assert.Throws<InvalidOperationException>(outer.Dispose);
        inner.Cancel();
        outer.Dispose(); inner.Dispose();
        Assert.False(root.TargetState);
        Assert.False(scope.IsUpdating);
        Assert.False(scope.IsPreparing);
        Assert.Equal(0, requests);
        root.OnFrame(Ms(0));
        Assert.False(root.CurrentState);
    }

    [Fact]
    public void ReverseDuringPreparation_InvalidatesLateCommitWithoutDroppingNewReservation()
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        var requests = new List<SharedTransitionPreparation>();
        scope.PreparationRequested += requests.Add;
        root.UpdateTarget(false);
        root.OnFrame(Ms(0));
        root.UpdateTarget(true);
        Assert.Equal(2, requests.Count);
        Assert.True(requests[0].CancellationToken.IsCancellationRequested);
        Assert.False(scope.TryCommitPreparation(requests[0].Generation, () => throw new Exception("stale callback executed")));
        Assert.False(scope.CancelPreparation(requests[0].Generation));
        root.OnFrame(Ms(16));
        Assert.True(root.IsPreparing);
        Assert.True(scope.CancelPreparation(requests[1].Generation));
        root.OnFrame(Ms(32));
        Assert.False(root.IsRunning);
    }

    [Fact]
    public void Commit_InstallsRealWorkBeforeReleasingReservations()
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        root.UpdateTarget(false);
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(1000));
        var installs = 0;
        Assert.True(scope.TryCommitPreparation(scope.Generation, () =>
        {
            Assert.True(root.IsPreparing);
            Assert.True(source.IsPreparing);
            source.AnimateDouble(s => s == EnterExitState.Visible ? 0 : 1,
                _ => new TweenSpec<double>(Ms(200), easing: new LinearEasing()));
            installs++;
        }));
        Assert.False(scope.IsPreparing);
        Assert.False(root.IsPreparing);
        Assert.False(scope.TryCommitPreparation(scope.Generation, () => installs++));
        root.OnFrame(Ms(1100));
        Assert.Equal(EnterExitState.Visible, source.CurrentState);
        root.OnFrame(Ms(1200));
        Assert.Equal(EnterExitState.PostExit, source.CurrentState);
        Assert.Equal(1, installs);
    }

    [Fact]
    public void LateRegistration_RepublishesSameIntent_WithDistinctIdentity()
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var first = scope.RegisterParticipant("photo", source);
        var requests = new List<SharedTransitionPreparation>();
        scope.PreparationRequested += requests.Add;
        root.UpdateTarget(false);
        using var target = Visibility(root, false);
        using var second = scope.RegisterParticipant("photo", target);
        Assert.Equal(2, requests.Count);
        Assert.Equal(requests[0].Generation, requests[1].Generation);
        Assert.Equal(2, requests[1].Participants.Count);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Single(requests[1].Before);
        second.Dispose();
        // Removing one side of a paired intent must not cancel the other side's preparation;
        // the runtime session/host decides whether to fall back or complete the remaining side.
        Assert.True(scope.IsPreparing);
        Assert.False(requests[0].CancellationToken.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeekOrParentDisposal_CancelsPendingRequestAndOpenTransaction(bool dispose)
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        root.UpdateTarget(false);
        var update = scope.BeginUpdate();
        if (dispose) root.Dispose();
        else root.SetPlaytimeAfterInitialAndTargetStateEstablished(true, false, Ms(50));
        update.Dispose();
        Assert.False(scope.IsUpdating);
        Assert.False(scope.IsPreparing);
        if (!dispose) Assert.Throws<InvalidOperationException>(() => scope.BeginUpdate());
    }

    [Fact]
    public void ParticipantTransitionDisposal_CancelsPreparation()
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        root.UpdateTarget(false);
        source.Dispose();
        Assert.False(participant.IsActive);
        Assert.False(scope.IsPreparing);
        Assert.False(root.IsPreparing);
    }

    [Fact]
    public void FailedInstall_ReleasesOnlyItsRequest_AndParentRemainsUsable()
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        root.UpdateTarget(false);
        Assert.Throws<InvalidOperationException>(() => scope.TryCommitPreparation(scope.Generation,
            () => throw new InvalidOperationException("backend preparation failed")));
        Assert.False(root.IsPreparing);
        scope.Dispose();
        root.OnFrame(Ms(0));
        root.UpdateTarget(true);
        root.OnFrame(Ms(16));
        Assert.True(root.CurrentState);
    }

    [Fact]
    public void ThrowingPreparationOrCancellationCallback_CannotStrandParentLeases()
    {
        using var root = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var source = Visibility(root);
        using var scope = SharedTransitionScope.Create(root);
        using var participant = scope.RegisterParticipant("photo", source);
        scope.PreparationRequested += request =>
        {
            request.CancellationToken.Register(() => throw new InvalidOperationException("cancel callback"));
            throw new InvalidOperationException("prepare callback");
        };
        root.UpdateTarget(false);
        Assert.NotNull(scope.LastPreparationError);
        Assert.False(scope.IsPreparing);
        Assert.False(root.IsPreparing);
        Assert.False(source.IsPreparing);
        root.OnFrame(Ms(0));
        Assert.False(root.IsRunning);
    }

    [Fact]
    public void SharedScope_DoesNotOwnParentLoop_AndRejectsUnrelatedParticipants()
    {
        var clock = new ManualAnimationFrameClock();
        using var root = new Transition<bool>(true, clock);
        using var unrelated = new Transition<bool>(true, clock);
        using var wrong = Visibility(unrelated);
        using var scope = SharedTransitionScope.Create(root);
        Assert.Throws<ArgumentException>(() => scope.RegisterParticipant("photo", wrong));
        using (scope.BeginUpdate()) root.UpdateTarget(false);
        Assert.False(scope.IsPreparing); // No participants: no asynchronous preparation work.
        Assert.Equal(0, clock.PendingFrameCount);
        scope.Dispose();
        root.OnFrame(Ms(0));
        Assert.False(root.CurrentState);
    }
}
