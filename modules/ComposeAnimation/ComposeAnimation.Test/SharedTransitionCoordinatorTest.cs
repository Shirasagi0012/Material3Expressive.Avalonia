using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SharedTransitionCoordinatorTest
{
    private static TimeSpan Ms(int value) => TimeSpan.FromMilliseconds(value);
    private static readonly Rect Start = new(10, 20, 100, 80);
    private static readonly Rect End = new(250, 100, 200, 160);

    private sealed class Backend : ISharedTransitionBackend
    {
        internal List<Presentation> Presentations { get; } = [];
        internal Action<SharedTransitionMotion>? Creating { get; set; }
        public ISharedTransitionPresentation Create(SharedTransitionMotion motion)
        {
            Creating?.Invoke(motion);
            var presentation = new Presentation(motion);
            Presentations.Add(presentation);
            return presentation;
        }
    }
    private sealed class Presentation(SharedTransitionMotion motion) : ISharedTransitionPresentation
    {
        internal SharedTransitionMotion Motion { get; } = motion;
        internal List<SharedTransitionHandoff> Handoffs { get; } = [];
        internal List<SharedSessionEndReason> Ends { get; } = [];
        internal int Retargets { get; private set; }
        public void Retarget(SharedTransitionPair pair) => Retargets++;
        public void HandoffReady(SharedTransitionHandoff handoff) => Handoffs.Add(handoff);
        public void End(SharedSessionEndReason reason) => Ends.Add(reason);
    }
    private sealed class Fixture : IDisposable
    {
        internal Transition<bool> Parent { get; } = new(true, new ManualAnimationFrameClock());
        internal Transition<EnterExitState> Source { get; }
        internal Transition<EnterExitState> Target { get; }
        internal SharedTransitionScope Scope { get; }
        internal Backend Backend { get; } = new();
        internal SharedTransitionCoordinator Coordinator { get; }
        internal Rect? SourceBounds = Start;
        internal Rect? TargetBounds = End;
        internal SharedTransitionParticipant From { get; }
        internal SharedTransitionParticipant? To { get; private set; }
        internal Fixture(bool registerTarget = true)
        {
            Source = Parent.CreateChildTransition(s => s ? EnterExitState.Visible : EnterExitState.PostExit);
            Target = Parent.CreateChildTransition(s => s ? EnterExitState.PreEnter : EnterExitState.Visible);
            Scope = SharedTransitionScope.Create(Parent);
            Coordinator = new(Scope, Backend, _ => new TweenSpec<Rect>(Ms(100), easing: new LinearEasing()));
            From = Coordinator.Register("photo", Source, () => SourceBounds);
            if (registerTarget) AddTarget();
        }
        internal void AddTarget() => To = Coordinator.Register("photo", Target, () => TargetBounds);
        internal SharedTransitionMotion StartMotion()
        {
            Parent.UpdateTarget(false);
            Coordinator.LayoutCompleted();
            return Assert.Single(Coordinator.Motions);
        }
        public void Dispose() { Coordinator.Dispose(); Scope.Dispose(); Parent.Dispose(); }
    }

    [Fact]
    public void CapturesSourceBeforeTargetChange_AndReadsDestinationAfterLayout()
    {
        using var f = new Fixture();
        f.Parent.UpdateTarget(false);
        Assert.Empty(f.Coordinator.Motions);
        f.SourceBounds = new Rect(900, 900, 5, 5);
        f.TargetBounds = End.WithX(300);
        f.Coordinator.LayoutCompleted();
        var motion = Assert.Single(f.Coordinator.Motions);
        Assert.Equal(Start, motion.Pair.SourceBounds);
        Assert.Equal(f.TargetBounds, motion.Pair.TargetBounds);
        Assert.Equal(Start, motion.Bounds.Value);
        Assert.False(f.Scope.IsPreparing);
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(100));
        var presentation = Assert.Single(f.Backend.Presentations);
        Assert.Equal(f.TargetBounds, motion.Bounds.Value);
        Assert.True(motion.TryComplete(Assert.Single(presentation.Handoffs)));
        Assert.Equal(SharedSessionEndReason.Completed, Assert.Single(presentation.Ends));
        f.Parent.OnFrame(Ms(116));
        Assert.False(f.Parent.IsRunning);
    }

    [Fact]
    public void LateTarget_RequiresLayoutAfterRegistration_AndUsesOriginalSourceSnapshot()
    {
        using var f = new Fixture(false);
        f.Parent.UpdateTarget(false);
        f.Coordinator.LayoutCompleted();
        Assert.Equal(SharedMatchFailure.MissingTarget, Assert.Single(f.Coordinator.LastResolution!.Diagnostics).Reason);
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(60000));
        Assert.Equal(EnterExitState.Visible, f.Source.CurrentState);
        f.SourceBounds = null;
        f.AddTarget();
        Assert.Empty(f.Coordinator.Motions);
        f.Coordinator.LayoutCompleted();
        var motion = Assert.Single(f.Coordinator.Motions);
        Assert.Equal(Start, motion.Pair.SourceBounds);
        Assert.False(f.Parent.IsPreparing);
        f.Parent.OnFrame(Ms(60050));
        Assert.InRange(motion.Bounds.Value.X, Start.X + 1, End.X - 1);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("nonfinite")]
    public void InvalidDestination_WaitsForUsableLayout(string kind)
    {
        using var f = new Fixture();
        f.TargetBounds = kind switch
        {
            "missing" => null,
            "empty" => new Rect(0, 0, 0, 80),
            _ => new Rect(double.NaN, 0, 100, 80)
        };
        f.Parent.UpdateTarget(false);
        f.Coordinator.LayoutCompleted();
        Assert.Empty(f.Backend.Presentations);
        Assert.True(f.Scope.IsPreparing);
        Assert.Equal(SharedMatchFailure.TargetLayoutUnavailable, Assert.Single(f.Coordinator.LastResolution!.Diagnostics).Reason);
        f.TargetBounds = End;
        f.Coordinator.LayoutCompleted();
        Assert.Single(f.Coordinator.Motions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DuplicateEligibleKey_FallsBackInsteadOfChoosingRegistrationOrder(bool duplicateSource)
    {
        using var f = new Fixture();
        using var duplicate = f.Coordinator.Register("photo", duplicateSource ? f.Source : f.Target, () => Start);
        f.Parent.UpdateTarget(false);
        f.Coordinator.LayoutCompleted();
        Assert.Equal(SharedMatchFailure.AmbiguousKey, Assert.Single(f.Coordinator.LastResolution!.Diagnostics).Reason);
        Assert.Empty(f.Backend.Presentations);
        Assert.False(f.Scope.IsPreparing);
        f.Parent.OnFrame(Ms(0));
        Assert.False(f.Parent.IsRunning);
    }

    [Fact]
    public void StableVisibleRegistration_DoesNotAnimateForUnrelatedParentIntent()
    {
        using var f = new Fixture();
        using var stableVisibility = f.Parent.CreateChildTransition(_ => EnterExitState.Visible);
        using var stable = f.Coordinator.Register("stable", stableVisibility, () => Start);
        f.StartMotion();
        Assert.Single(f.Backend.Presentations);
        Assert.Empty(f.Coordinator.LastResolution!.Diagnostics);
    }

    [Fact]
    public void ReadyPairs_InstallWithoutWaitingForUnmatchedKeys()
    {
        using var f = new Fixture();
        using var missing = f.Coordinator.Register("title", f.Source, () => Start);
        f.Parent.UpdateTarget(false);
        var generation = f.Scope.Generation;
        f.Coordinator.LayoutCompleted();
        Assert.Single(f.Coordinator.Motions);
        Assert.False(f.Scope.IsPreparing);
        Assert.Equal(SharedMatchFailure.MissingTarget, Assert.Single(f.Coordinator.LastResolution!.Diagnostics).Reason);
        Assert.False(f.Coordinator.ExpirePreparation(generation));
    }

    [Fact]
    public void DeadlineBeforeLayout_CancelsWithoutCapturingUnlaidOutTarget()
    {
        using var f = new Fixture();
        f.Parent.UpdateTarget(false);
        Assert.True(f.Coordinator.ExpirePreparation(f.Scope.Generation));
        Assert.Empty(f.Coordinator.Motions);
        Assert.False(f.Scope.IsPreparing);
    }

    [Fact]
    public void OldDeadline_CannotCancelReversedIntent()
    {
        using var f = new Fixture();
        var motion = f.StartMotion();
        var oldGeneration = f.Scope.Generation;
        f.Parent.UpdateTarget(true);
        Assert.False(f.Coordinator.ExpirePreparation(oldGeneration));
        Assert.True(f.Scope.IsPreparing);
        f.Coordinator.LayoutCompleted();
        Assert.Same(motion, Assert.Single(f.Coordinator.Motions));
    }

    [Fact]
    public void Reverse_ReusesSessionMotionPresentationAndPreservesTrajectory()
    {
        using var f = new Fixture();
        var motion = f.StartMotion();
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(40));
        var value = motion.Bounds.Value;
        var velocity = motion.Bounds.VelocityVector.ToArray();
        f.Parent.UpdateTarget(true);
        f.Coordinator.LayoutCompleted();
        Assert.Same(motion, Assert.Single(f.Coordinator.Motions));
        Assert.Same(f.To, motion.Pair.Source);
        Assert.Same(f.From, motion.Pair.Target);
        Assert.Equal(value, motion.Pair.SourceBounds);
        Assert.Equal(value, motion.Bounds.Value);
        Assert.Equal(velocity, motion.Bounds.VelocityVector.ToArray());
        Assert.Equal(value, motion.Bounds.Animation.GetValue(f.Parent.PlayTime));
        Assert.Equal(1, Assert.Single(f.Backend.Presentations).Retargets);
    }

    [Fact]
    public void LayoutChanges_PreserveVelocity_AndInvalidateOldHandoffWithinSameGeneration()
    {
        using var f = new Fixture();
        var motion = f.StartMotion();
        var presentation = Assert.Single(f.Backend.Presentations);
        f.Parent.OnFrame(Ms(0));
        f.Parent.OnFrame(Ms(40));
        var value = motion.Bounds.Value;
        var velocity = motion.Bounds.VelocityVector.ToArray();
        f.TargetBounds = End.WithX(400);
        f.Coordinator.LayoutCompleted();
        Assert.Equal(value, motion.Bounds.Value);
        Assert.Equal(velocity, motion.Bounds.VelocityVector.ToArray());
        var trajectory = motion.Bounds.Animation;
        f.Coordinator.LayoutCompleted();
        Assert.Same(trajectory, motion.Bounds.Animation);
        f.Parent.OnFrame(motion.Bounds.Duration);
        var oldHandoff = Assert.Single(presentation.Handoffs);
        f.TargetBounds = End.WithX(500);
        f.Coordinator.LayoutCompleted();
        Assert.False(motion.TryComplete(oldHandoff));
        f.Parent.OnFrame(motion.Bounds.Duration);
        Assert.Equal(2, presentation.Handoffs.Count);
        Assert.False(motion.TryComplete(oldHandoff));
        Assert.True(motion.TryComplete(presentation.Handoffs[1]));
    }

    [Fact]
    public void MissingSourceLayout_FallsBackRatherThanReadingSourceAfterStateChange()
    {
        using var f = new Fixture();
        f.SourceBounds = null;
        f.Parent.UpdateTarget(false);
        f.SourceBounds = Start;
        f.Coordinator.LayoutCompleted();
        Assert.Equal(SharedMatchFailure.SourceLayoutUnavailable, Assert.Single(f.Coordinator.LastResolution!.Diagnostics).Reason);
        Assert.False(f.Scope.IsPreparing);
        Assert.Empty(f.Coordinator.Motions);
    }

    [Fact]
    public void BackendCreationFailure_CleansEarlierPairsAndAllowsLaterNavigation()
    {
        using var f = new Fixture();
        using var titleFrom = f.Coordinator.Register("title", f.Source, () => Start);
        using var titleTo = f.Coordinator.Register("title", f.Target, () => End);
        f.Backend.Creating = m => { if (Equals(m.Pair.Source.Key, "title")) throw new InvalidOperationException("capture failed"); };
        f.Parent.UpdateTarget(false);
        f.Coordinator.LayoutCompleted();
        Assert.Empty(f.Coordinator.Motions);
        Assert.Empty(f.Scope.Sessions);
        Assert.NotNull(f.Scope.LastPreparationError);
        Assert.Equal(SharedSessionEndReason.Failed, Assert.Single(Assert.Single(f.Backend.Presentations).Ends));
        Assert.False(f.Parent.IsPreparing);
        f.Backend.Creating = null;
        f.Parent.UpdateTarget(true);
        f.Coordinator.LayoutCompleted();
        Assert.Equal(2, f.Coordinator.Motions.Count);
    }

    [Fact]
    public void ReentrantEndpointRemovalDuringBackendCreation_CleansReturnedPresentation()
    {
        using var f = new Fixture();
        f.Backend.Creating = _ => f.From.Dispose();
        f.Parent.UpdateTarget(false);
        f.Coordinator.LayoutCompleted();
        Assert.Empty(f.Coordinator.Motions);
        Assert.Empty(f.Scope.Sessions);
        Assert.Single(Assert.Single(f.Backend.Presentations).Ends);
        Assert.False(f.Parent.IsPreparing);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TeardownReleasesPresentationsAndRegistrationsWithoutDisposingParent(bool disposeScope)
    {
        using var f = new Fixture();
        f.StartMotion();
        if (disposeScope) f.Scope.Dispose();
        else f.Coordinator.Dispose();
        Assert.Single(Assert.Single(f.Backend.Presentations).Ends);
        Assert.Empty(f.Scope.Sessions);
        Assert.False(f.From.IsActive);
        Assert.False(f.To!.IsActive);
        f.Parent.OnFrame(Ms(0));
        Assert.False(f.Parent.IsRunning);
    }

    [Fact]
    public void InvalidTargetAfterStart_CancelsInsteadOfHandingOffToOldGeometry()
    {
        using var f = new Fixture();
        var motion = f.StartMotion();
        f.TargetBounds = null;
        f.Coordinator.LayoutCompleted();
        Assert.True(motion.Session.IsClosed);
        Assert.Empty(f.Coordinator.Motions);
        Assert.Equal(SharedSessionEndReason.Cancelled, Assert.Single(Assert.Single(f.Backend.Presentations).Ends));
    }

    [Fact]
    public void ExplicitUpdateReturningToOriginalTarget_DoesNotCreateMotion()
    {
        using var f = new Fixture();
        using (f.Scope.BeginUpdate())
        {
            f.Parent.UpdateTarget(false);
            f.Parent.UpdateTarget(true);
        }
        f.Coordinator.LayoutCompleted();
        Assert.Empty(f.Coordinator.Motions);
        Assert.Empty(f.Coordinator.LastResolution!.Diagnostics);
        Assert.False(f.Parent.IsPreparing);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public Task RealLayout_LateVisibilityScopePairsAndRetargetsWithinOneHost(bool reverse, bool zeroDuration)
        => HeadlessApp.Session.Dispatch(() =>
    {
        using var parent = new Transition<bool>(true, new ManualAnimationFrameClock());
        using var scope = SharedTransitionScope.Create(parent);
        var backend = new Backend();
        using var coordinator = new SharedTransitionCoordinator(scope, backend,
            _ => new TweenSpec<Rect>(Ms(100), easing: new LinearEasing()));
        var stage = new Canvas();
        var sourceContent = new Border { Width = 100, Height = 80, Background = Brushes.Blue };
        var targetContent = new Border { Width = 200, Height = 160, Background = Brushes.Red };
        var source = new AnimatedVisibility
        {
            Content = sourceContent, Source = VisibilitySource.From(parent, s => s),
            Enter = EnterTransition.None, Exit = ExitTransition.None
        };
        var target = new AnimatedVisibility
        {
            Content = targetContent, Source = VisibilitySource.From(parent, s => !s),
            Enter = EnterTransition.None, Exit = ExitTransition.None
        };
        Canvas.SetLeft(source, 10); Canvas.SetTop(source, 20);
        Canvas.SetLeft(target, 250); Canvas.SetTop(target, 100);
        Rect? ReadLayout(Control control)
        {
            if (!control.IsMeasureValid || !control.IsArrangeValid || TopLevel.GetTopLevel(control) is null) return null;
            var transform = control.TransformToVisual(stage);
            return transform is { } value ? new Rect(control.Bounds.Size).TransformToAABB(value) : null;
        }
        source.ScopeCreated += (_, e) => e.Scope.Own(coordinator.Register("photo", e.Scope.Transition, () => ReadLayout(sourceContent)));
        target.ScopeCreated += (_, e) => e.Scope.Own(coordinator.Register("photo", e.Scope.Transition, () => ReadLayout(targetContent)));
        stage.Children.Add(source); stage.Children.Add(target);
        var window = new AnimationTestWindow() { Width = 800, Height = 600, Content = stage };
        void Layout() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
        try
        {
            window.Show(); Layout(); parent.OnFrame(TimeSpan.Zero);
            Assert.Null(target.Scope);
            parent.DurationScale = zeroDuration ? 0 : 1;
            parent.UpdateTarget(false);
            Assert.Empty(coordinator.Motions);
            Layout(); coordinator.LayoutCompleted();
            var motion = Assert.Single(coordinator.Motions);
            Assert.Equal(Start, motion.Pair.SourceBounds);
            Assert.Equal(End, motion.Pair.TargetBounds);
            var sourceScope = source.Scope;
            var targetScope = target.Scope;
            parent.OnFrame(Ms(16)); parent.OnFrame(Ms(56));
            if (reverse)
            {
                var sampled = motion.Bounds.Value;
                parent.UpdateTarget(true);
                Layout(); coordinator.LayoutCompleted();
                Assert.Same(motion, Assert.Single(coordinator.Motions));
                Assert.Equal(sampled, motion.Bounds.Value);
                Assert.Equal(Start, motion.Pair.TargetBounds);
            }
            else if (!zeroDuration)
            {
                var sampled = motion.Bounds.Value;
                targetContent.Width = 260;
                Layout(); coordinator.LayoutCompleted();
                Assert.Equal(260, motion.Pair.TargetBounds.Width);
                Assert.Equal(sampled, motion.Bounds.Value);
            }
            var finish = Ms(16) + motion.Bounds.Duration;
            parent.OnFrame(finish); Layout();
            Assert.Same(sourceScope, source.Scope);
            Assert.Same(targetScope, target.Scope);
            Assert.True(source.IsContentPresent);
            Assert.True(target.IsContentPresent);
            var presentation = Assert.Single(backend.Presentations);
            Assert.True(motion.TryComplete(presentation.Handoffs.Last()));
            parent.OnFrame(finish + Ms(16)); Layout();
            Assert.Equal(reverse, source.IsContentPresent);
            Assert.Equal(!reverse, target.IsContentPresent);
            Assert.False(parent.IsRunning);
            Assert.Equal(SharedSessionEndReason.Completed, Assert.Single(presentation.Ends));
            Assert.Null(scope.LastSessionError);
            Assert.Null(scope.LastPreparationError);
        }
        finally { window.Close(); }
    }, CancellationToken.None);

    [Fact]
    public void OneCoordinatorPerScope_AndAttachMustPrecedePreparation()
    {
        using var f = new Fixture();
        Assert.Throws<InvalidOperationException>(() => new SharedTransitionCoordinator(f.Scope, new Backend()));
        f.Coordinator.Dispose();
        using var registration = f.Scope.RegisterParticipant("x", f.Source);
        f.Parent.UpdateTarget(false);
        Assert.Throws<InvalidOperationException>(() => new SharedTransitionCoordinator(f.Scope, new Backend()));
    }
}
