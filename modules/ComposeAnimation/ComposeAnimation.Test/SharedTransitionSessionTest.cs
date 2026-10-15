using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SharedTransitionSessionTest
{
    private static TimeSpan Ms(int value) => TimeSpan.FromMilliseconds(value);

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public Transition<bool> Parent { get; }
        public Transition<EnterExitState> Source { get; }
        public Transition<EnterExitState> Target { get; }
        public SharedTransitionScope Scope { get; }
        public SharedTransitionParticipant From { get; }
        public SharedTransitionParticipant To { get; }
        public Fixture()
        {
            Parent = new(true);
            Source = Parent.CreateChildTransition(s => s ? EnterExitState.Visible : EnterExitState.PostExit);
            Target = Parent.CreateChildTransition(s => s ? EnterExitState.PostExit : EnterExitState.Visible);
            Scope = SharedTransitionScope.Create(Parent);
            From = Scope.RegisterParticipant("photo", Source);
            To = Scope.RegisterParticipant("photo", Target);
        }
        public SharedTransitionSession Start(Action<SharedTransitionSession>? configure = null,
            Action<SharedTransitionSession, SharedSessionEndReason>? ending = null)
        {
            Parent.UpdateTarget(false);
            SharedTransitionSession? session = null;
            Assert.True(Scope.TryCommitSessions(Scope.Generation, batch =>
                session = batch.Start(From, To, configure ?? (s => Motion(s)), ending ?? ((_, _) => { }))));
            return session!;
        }
        public void Dispose() { Scope.Dispose(); Parent.Dispose(); }
    }

    private static Transition<int>.TransitionAnimationState<double> Motion(SharedTransitionSession session, int duration = 100)
        => session.Transition.CreateDeferredAnimation(new DoubleConverter()).Animate(
            _ => new TweenSpec<double>(Ms(duration), easing: new LinearEasing()), 0, null, _ => 1);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FinishedAnimation_WaitsForDrawingHandoffBeforeReleasingEndpoints(bool zeroDuration)
    {
        using var f = new Fixture();
        if (zeroDuration) f.Parent.DurationScale = 0;
        var handoffs = 0;
        var ready = 0;
        f.Scope.HandoffReady += _ => ready++;
        var session = f.Start(ending: (_, reason) =>
        {
            Assert.Equal(SharedSessionEndReason.Completed, reason);
            Assert.Contains(f.Source.Animations, a => !a.IsFinished);
            Assert.Contains(f.Target.Animations, a => !a.IsFinished);
            Assert.Equal(EnterExitState.Visible, f.Source.CurrentState);
            handoffs++;
        });
        Assert.Same(f.Parent, session.Transition.ParentTransition);
        Assert.False(f.Parent.IsPreparing);
        Assert.Equal(0, f.Frames.PendingFrameCount);
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(100));
        Assert.Equal(SharedSessionState.AwaitingHandoff, session.State);
        Assert.True(f.Parent.IsRunning);
        Assert.Equal(EnterExitState.Visible, f.Source.CurrentState);
        f.Parent.OnFrame(Ms(60000));
        Assert.Equal(1, ready);
        Assert.Equal(0, handoffs);
        Assert.True(session.TryComplete(session.Generation));
        Assert.False(session.TryComplete(session.Generation));
        Assert.Equal(1, handoffs);
        Assert.Empty(f.Source.Animations);
        Assert.Empty(f.Target.Animations);
        f.Parent.OnFrame(Ms(60016));
        Assert.False(f.Parent.IsRunning);
        Assert.Equal(EnterExitState.PostExit, f.Source.CurrentState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptySession_NotifiesOnlyAfterPreparationCommit(bool insideNotification)
    {
        using var f = new Fixture();
        SharedTransitionSession? session = null;
        var ready = 0;
        f.Scope.HandoffReady += value =>
        {
            Assert.False(f.Scope.IsPreparing);
            Assert.Contains(f.Source.Animations, a => !a.IsFinished);
            Assert.Contains(f.Target.Animations, a => !a.IsFinished);
            ready++;
            Assert.True(value.TryComplete(value.Generation));
        };
        void Install() => Assert.True(f.Scope.TryCommitSessions(f.Scope.Generation,
            batch => session = batch.Start(f.From, f.To, _ => { }, (_, _) => { })));
        if (insideNotification) f.Scope.PreparationRequested += _ => Install();
        f.Parent.UpdateTarget(false);
        if (!insideNotification) Install();
        Assert.Equal(1, ready);
        Assert.Equal(SharedSessionState.Completed, session!.State);
        f.Parent.OnFrame(Ms(0));
        Assert.False(f.Parent.IsRunning);
    }

    [Fact]
    public void Reverse_ReusesSessionTimelineAnimationAndSampledVelocity()
    {
        using var f = new Fixture();
        Transition<int>.TransitionAnimationState<double> motion = null!;
        var session = f.Start(s => motion = s.Transition.CreateDeferredAnimation(new DoubleConverter()).Animate(
            _ => new SpringSpec<double>(stiffness: 16), 0, null, _ => 100));
        var owner = session.Transition;
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(240));
        var before = motion.Value;
        var velocity = motion.VelocityVector.ToArray();
        Assert.True(velocity[0] > 0);
        var trajectory = motion.Animation;
        f.Parent.UpdateTarget(true);
        Assert.Same(trajectory, motion.Animation); // Changing the parent cannot reconfigure this stable child.
        Assert.True(f.Scope.TryCommitSessions(f.Scope.Generation,
            batch => batch.Retarget(session, f.To, f.From, () => motion.Update(_ => 0))));
        Assert.Same(owner, session.Transition);
        Assert.Same(motion, Assert.Single(owner.Animations));
        Assert.Same(f.From, session.Target);
        Assert.Equal(before, motion.Value);
        Assert.Equal(velocity, motion.VelocityVector.ToArray());
        Assert.Equal(before, motion.Animation.GetValue(f.Parent.PlayTime), 10);
        var derivative = new double[1];
        motion.Animation.GetVelocityVector(f.Parent.PlayTime, derivative);
        Assert.Equal(velocity[0], derivative[0], 10);
        f.Parent.OnFrame(Ms(241));
        Assert.True(motion.Value > before);
        f.Parent.OnFrame(motion.Duration);
        Assert.Equal(0, motion.Value);
        Assert.True(session.TryComplete(session.Generation));
        f.Parent.OnFrame(motion.Duration + Ms(16));
        Assert.True(f.Parent.CurrentState);
        Assert.False(f.Parent.IsRunning);
    }

    [Fact]
    public void MultiplePairs_CommitTogether_AndRetainSharedVisibilityUntilLastHandoff()
    {
        using var f = new Fixture();
        using var titleFrom = f.Scope.RegisterParticipant("title", f.Source);
        using var titleTo = f.Scope.RegisterParticipant("title", f.Target);
        f.Parent.UpdateTarget(false);
        SharedTransitionSession first = null!, second = null!;
        Assert.True(f.Scope.TryCommitSessions(f.Scope.Generation, batch =>
        {
            first = batch.Start(f.From, f.To, s => Motion(s, 100), (_, _) => { });
            Assert.True(f.Scope.IsPreparing);
            second = batch.Start(titleFrom, titleTo, s => Motion(s, 300), (_, _) => { });
        }));
        Assert.Equal(Ms(300), f.Parent.TotalDuration);
        Assert.Equal(Ms(300), f.Source.TotalDuration);
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(100));
        Assert.True(first.TryComplete(first.Generation));
        Assert.False(second.TryComplete(second.Generation));
        f.Parent.OnFrame(Ms(200));
        Assert.Equal(EnterExitState.Visible, f.Source.CurrentState);
        f.Parent.OnFrame(Ms(300));
        Assert.True(second.TryComplete(second.Generation));
        f.Parent.OnFrame(Ms(316));
        Assert.False(f.Parent.IsRunning);
    }

    [Fact]
    public void SessionWaitsForLongestContentAnimation_ButDoesNotWaitForOrdinaryEnterExitEffects()
    {
        using var f = new Fixture();
        var exit = f.Source.AnimateDouble(s => s == EnterExitState.Visible ? 1 : 0,
            _ => new TweenSpec<double>(Ms(400)));
        var session = f.Start(s => { Motion(s, 40); Motion(s, 200); });
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(100));
        Assert.Equal(SharedSessionState.Running, session.State);
        f.Parent.OnFrame(Ms(200));
        Assert.True(session.TryComplete(session.Generation));
        Assert.False(exit.IsFinished);
        f.Parent.OnFrame(Ms(300));
        Assert.Equal(EnterExitState.Visible, f.Source.CurrentState);
        f.Parent.OnFrame(Ms(400));
        Assert.Equal(EnterExitState.PostExit, f.Source.CurrentState);
    }

    [Fact]
    public void NewIntent_RejectsOldHandoff_AndCancelledPreparationReleasesOldSession()
    {
        using var f = new Fixture();
        var session = f.Start();
        var generation = session.Generation;
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(100));
        f.Parent.UpdateTarget(true);
        Assert.False(session.TryComplete(generation));
        Assert.False(f.Scope.TryCommitSessions(generation, _ => throw new Exception("stale install")));
        Assert.True(f.Scope.CancelPreparation(f.Scope.Generation));
        Assert.Equal(SharedSessionEndReason.Superseded, session.EndReason);
        Assert.Empty(f.Scope.Sessions);
        f.Parent.OnFrame(Ms(116));
        Assert.False(f.Parent.IsRunning);
    }

    [Fact]
    public void EmptyNewBatch_CancelsUnmatchedOldPairs()
    {
        using var f = new Fixture();
        var session = f.Start();
        f.Parent.UpdateTarget(true);
        Assert.True(f.Scope.TryCommitSessions(f.Scope.Generation, _ => { }));
        Assert.Equal(SharedSessionEndReason.Superseded, session.EndReason);
        f.Parent.OnFrame(Ms(0));
        Assert.False(f.Parent.IsRunning);
    }

    [Theory]
    [InlineData("participant")]
    [InlineData("visibility")]
    [InlineData("scope")]
    [InlineData("parent")]
    [InlineData("seeking")]
    public void Teardown_CleansSessionsAndEndpointLinks(string action)
    {
        using var f = new Fixture();
        var ends = 0;
        var session = f.Start(ending: (_, _) => ends++);
        f.Parent.OnFrame(Ms(0));
        switch (action)
        {
            case "participant": f.From.Dispose(); break;
            case "visibility": f.Source.Dispose(); break;
            case "scope": f.Scope.Dispose(); break;
            case "parent": f.Parent.Dispose(); break;
            case "seeking": f.Parent.SetPlaytimeAfterInitialAndTargetStateEstablished(true, false, Ms(50)); break;
        }
        session.Dispose();
        Assert.Equal(1, ends);
        Assert.True(session.IsClosed);
        Assert.True(session.Transition.IsDisposed);
        Assert.Empty(f.Scope.Sessions);
        Assert.Empty(f.Target.Animations);
        Assert.Null(f.Scope.LastSessionError);
    }

    [Theory]
    [InlineData("install")]
    [InlineData("ready")]
    [InlineData("ending")]
    public void CallbackFailure_CannotStrandRetentionOrBreakParentNotificationQueue(string callback)
    {
        using var f = new Fixture();
        SharedTransitionSession? session = null;
        if (callback == "ready") f.Scope.HandoffReady += _ => throw new InvalidOperationException("ready");
        else f.Scope.HandoffReady += s => s.TryComplete(s.Generation);
        f.Scope.PreparationRequested += _ => f.Scope.TryCommitSessions(f.Scope.Generation, batch =>
            batch.Start(f.From, f.To, s =>
            {
                session = s;
                Motion(s);
                if (callback == "install") throw new InvalidOperationException("install");
            }, (_, _) => { if (callback == "ending") throw new InvalidOperationException("ending"); }));
        f.Parent.UpdateTarget(false);
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(100));
        f.Parent.OnFrame(Ms(116));
        Assert.NotNull(session);
        Assert.True(session.IsClosed);
        Assert.False(f.Parent.IsPreparing);
        Assert.False(f.Parent.IsRunning);
        Assert.Empty(f.Scope.Sessions);
        Assert.Empty(f.Source.Animations);
        Assert.Empty(f.Target.Animations);
        if (callback == "install") Assert.NotNull(f.Scope.LastPreparationError);
        else Assert.NotNull(f.Scope.LastSessionError);
    }

    [Fact]
    public void FinishedSession_RetargetedInsidePreparationNotificationCannotHandoffBeforeQueuedUpdate()
    {
        using var f = new Fixture();
        Transition<int>.TransitionAnimationState<double> motion = null!;
        var session = f.Start(s => motion = Motion(s));
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(100));
        Assert.Equal(SharedSessionState.AwaitingHandoff, session.State);
        var ready = 0;
        f.Scope.HandoffReady += _ => ready++;
        f.Scope.PreparationRequested += request => Assert.True(f.Scope.TryCommitSessions(request.Generation,
            batch => batch.Retarget(session, f.To, f.From, () => motion.Update(_ => 0))));
        f.Parent.UpdateTarget(true);
        Assert.Equal(0, ready);
        Assert.False(session.TryComplete(session.Generation));
        f.Parent.OnFrame(motion.Duration);
        Assert.Equal(1, ready);
        Assert.True(session.TryComplete(session.Generation));
    }

    [Fact]
    public void FailedSecondPair_RollsBackWholeInstallation_AndEscapedBatchCannotBeReused()
    {
        using var f = new Fixture();
        using var titleFrom = f.Scope.RegisterParticipant("title", f.Source);
        using var titleTo = f.Scope.RegisterParticipant("title", f.Target);
        f.Parent.UpdateTarget(false);
        SharedTransitionSessionBatch escaped = null!;
        Assert.Throws<InvalidOperationException>(() => f.Scope.TryCommitSessions(f.Scope.Generation, batch =>
        {
            escaped = batch;
            batch.Start(f.From, f.To, s => Motion(s), (_, _) => { });
            batch.Start(titleFrom, titleTo, _ => throw new InvalidOperationException("second pair"), (_, _) => { });
        }));
        Assert.Throws<ObjectDisposedException>(() => escaped.Start(f.From, f.To, _ => { }, (_, _) => { }));
        Assert.Empty(f.Scope.Sessions);
        Assert.Empty(f.Source.Animations);
        Assert.Empty(f.Target.Animations);
        f.Parent.OnFrame(Ms(0));
        Assert.False(f.Parent.IsRunning);
    }

    [Fact]
    public void DisposingAncestorOfSharedParent_DoesNotMutateDisposingEndpointTree()
    {
        using var root = new Transition<bool>(true);
        var parent = root.CreateChildTransition(s => s);
        var source = parent.CreateChildTransition(s => s ? EnterExitState.Visible : EnterExitState.PostExit);
        var target = parent.CreateChildTransition(s => s ? EnterExitState.PostExit : EnterExitState.Visible);
        using var scope = SharedTransitionScope.Create(parent);
        var from = scope.RegisterParticipant("photo", source);
        var to = scope.RegisterParticipant("photo", target);
        root.UpdateTarget(false);
        SharedTransitionSession session = null!;
        Assert.True(scope.TryCommitSessions(scope.Generation,
            batch => session = batch.Start(from, to, s => Motion(s), (_, _) => { })));
        root.Dispose();
        Assert.True(session.IsClosed);
        Assert.Empty(scope.Sessions);
        Assert.False(from.IsActive);
        Assert.False(to.IsActive);
        Assert.Null(scope.LastSessionError);
    }

    [Fact]
    public void ReentrantInstall_CannotCommitOverNewerPreparation()
    {
        using var f = new Fixture();
        f.Parent.UpdateTarget(false);
        var generation = f.Scope.Generation;
        SharedTransitionSession? session = null;
        Assert.False(f.Scope.TryCommitSessions(generation, batch =>
        {
            session = batch.Start(f.From, f.To, s => Motion(s), (_, _) => { });
            f.Parent.UpdateTarget(true);
        }));
        Assert.True(session!.IsClosed);
        Assert.True(f.Scope.IsPreparing);
        Assert.True(f.Parent.IsPreparing);
        Assert.True(f.Scope.CancelPreparation(f.Scope.Generation));
        f.Parent.OnFrame(Ms(0));
        Assert.False(f.Parent.IsRunning);
    }
}
