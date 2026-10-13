using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimatedStateTest
{
    private static TimeSpan Ms(double value) => TimeSpan.FromMilliseconds(value);
    private static TweenSpec<double> Linear(double duration = 100) => new(Ms(duration), easing: new LinearEasing());

    [Fact]
    public Task InitialAndEqualTargets_AreIdleEvenWithNewSpecInstances() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 10d);
        var finished = 0;
        state.Finished += _ => finished++;
        for (var i = 0; i < 100; i++) state.UpdateTarget(10, Linear());
        Assert.Equal(10, state.Value);
        Assert.False(state.IsRunning);
        Assert.Equal(0, finished);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task Updates_CoalesceWithoutEvaluatingSpecs_AndJoinFirstFrame() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d);
        var unused = new CountingSpec();
        var used = new CountingSpec();
        var values = new List<double>();
        state.ValueChanged += values.Add;
        state.UpdateTarget(100, unused);
        state.UpdateTarget(80, used);
        Assert.Equal(80, state.TargetValue);
        Assert.Equal(0, state.Value);
        Assert.Empty(values);
        Assert.Equal(0, used.Calls);
        Assert.Equal(1, clock.PendingFrameCount);
        clock.Pump(Ms(0));
        Assert.Equal(0, unused.Calls);
        Assert.Equal(1, used.Calls);
        Assert.True(state.IsRunning);
        clock.Pump(Ms(50));
        Assert.Equal(40, state.Value, 7);
        clock.Pump(Ms(50));
        Assert.Equal(80, state.Value);
        Assert.False(state.IsRunning);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task ReturningToUnchangedRunningTarget_DoesNotRestartIt() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d);
        var spec = new CountingSpec();
        state.UpdateTarget(100, spec);
        clock.Pump(Ms(0));
        clock.Pump(Ms(40));
        state.UpdateTarget(200, Linear(1000));
        state.UpdateTarget(100, new CountingSpec());
        clock.Pump(Ms(20));
        Assert.Equal(60, state.Value, 7);
        Assert.Equal(1, spec.Calls);
        clock.Pump(Ms(40));
        Assert.Equal(100, state.Value);
        window.Close();
    });

    [Fact]
    public Task SpecOnlyUpdate_DoesNotRestart_AndOmissionUsesDefault() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d, defaultAnimationSpec: Linear(200));
        state.UpdateTarget(100, Linear());
        clock.Pump(Ms(0));
        clock.Pump(Ms(40));
        state.UpdateTarget(100, Linear(1000));
        clock.Pump(Ms(60));
        Assert.Equal(100, state.Value);
        state.UpdateTarget(0);
        clock.Pump(Ms(0));
        clock.Pump(Ms(100));
        Assert.Equal(50, state.Value, 7);
        clock.Pump(Ms(100));
        window.Close();
    });

    [Fact]
    public Task FrameCallbackUpdate_SuppressesOldFinished_AndKeepsWakeDemand() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d);
        var finished = new List<double>();
        state.Finished += finished.Add;
        state.ValueChanged += value => { if (value == 100) state.UpdateTarget(200, Linear()); };
        state.UpdateTarget(100, Linear());
        clock.Pump(Ms(0));
        clock.Pump(Ms(100));
        Assert.Empty(finished);
        Assert.Equal(100, state.Value);
        Assert.Equal(1, clock.PendingFrameCount);
        clock.Pump(Ms(0));
        clock.Pump(Ms(100));
        Assert.Equal(new[] { 200d }, finished);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task CommitCallbackUpdatesAnotherState_AreDeferredToNextSnapshot() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var first = AnimatedState.Create(window.AnimationRoot, 0d);
        using var second = AnimatedState.Create(window.AnimationRoot, 0d);
        first.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(first.IsRunning) && first.IsRunning)
                second.UpdateTarget(200, Linear());
        };
        first.UpdateTarget(100, Linear());
        second.UpdateTarget(100, Linear());
        clock.Pump(Ms(0));
        Assert.Equal(200, second.TargetValue);
        clock.Pump(Ms(50));
        Assert.Equal(100, second.Value, 7); // the next batch retargets from the t=0 pose
        clock.Pump(Ms(50));
        Assert.Equal(200, second.Value);
        window.Close();
    });

    [Fact]
    public Task PropertyConnections_PreserveBaseValues_AndShareOneRuntime() => OnUi(() =>
    {
        var host = new Border();
        using var scene = new ReparentFixture(host);
        var other = new Border { Opacity = .9 };
        scene.Right.Children.Add(other);
        host.Opacity = .8;
        using var state = AnimatedState.Create(host, .2d);
        using var first = state.Bind(host, Visual.OpacityProperty);
        using var second = state.Bind(other, Visual.OpacityProperty);
        Assert.Equal(.2, host.Opacity);
        Assert.Equal(.8, host.GetBaseValue(Visual.OpacityProperty).Value);
        Assert.Equal(0, scene.Frames.PendingFrameCount);
        state.UpdateTarget(.6, Linear());
        scene.Step(0);
        scene.Step(50);
        Assert.Equal(.4, host.Opacity, 7);
        Assert.Equal(host.Opacity, other.Opacity);
        Assert.Equal(1, AnimationScope.Get(host).Diagnostics.PendingAnimationCount);
        first.Dispose();
        Assert.Equal(.8, host.Opacity);
        scene.Step(50);
        Assert.Equal(.6, other.Opacity);
        second.Dispose();
        Assert.Equal(.9, other.Opacity);
    });

    [Fact]
    public Task TargetDetachAndTemplateReplacement_DoNotResetSource() => OnUi(() =>
    {
        var owner = new Border();
        using var scene = new ReparentFixture(owner);
        var target = new Border { Opacity = .9 };
        scene.Right.Children.Add(target);
        using var state = AnimatedState.Create(owner, 0d);
        var connection = state.Bind(target, Visual.OpacityProperty);
        state.UpdateTarget(1, Linear());
        scene.Step(0);
        scene.Step(40);
        scene.Right.Children.Remove(target);
        Assert.Equal(.9, target.Opacity);
        scene.Step(20);
        Assert.Equal(.6, state.Value, 7);
        scene.Right.Children.Add(target);
        Assert.Equal(state.Value, target.Opacity);
        connection.Dispose();
        scene.Right.Children.Remove(target);
        var replacement = new Border { Opacity = .8 };
        scene.Right.Children.Add(replacement);
        using var newConnection = state.Bind(replacement, Visual.OpacityProperty);
        Assert.Equal(.6, replacement.Opacity, 7);
        scene.Step(40);
        Assert.Equal(1, replacement.Opacity);
        Assert.Equal(.9, target.Opacity);
    });

    [Fact]
    public Task SameRootReparent_RestoresBindingBeforeSampling_AndKeepsVelocityTimebase() => OnUi(() =>
    {
        var host = new Border { Opacity = .9 };
        using var scene = new ReparentFixture(host);
        using var state = AnimatedState.Create(host, 0d);
        using var connection = state.Bind(host, Visual.OpacityProperty);
        state.UpdateTarget(1, Linear());
        scene.Step(0);
        scene.Step(40);
        var oldScope = AnimationScope.Get(host);
        var sawFrozenContribution = false;
        AnimationScope.AddScopeChangedHandler(host, (_, e) =>
        {
            if (e.NewScope is not null)
                sawFrozenContribution = Math.Abs(host.Opacity - .4) < 1e-7;
        });
        scene.Move();
        scene.Step(20);
        Assert.NotSame(oldScope, AnimationScope.Get(host));
        Assert.True(sawFrozenContribution);
        Assert.Equal(.52, state.Value, 7); // new tween starts at .4 on the old 40ms timebase
        Assert.Equal(state.Value, host.Opacity);
    });

    [Fact]
    public Task TrueDetach_FreezesAndDoesNotWake_UntilReattach() => OnUi(() =>
    {
        var host = new Border();
        using var scene = new ReparentFixture(host);
        using var state = AnimatedState.Create(host, 0d);
        state.UpdateTarget(100, Linear());
        scene.Step(0);
        scene.Step(40);
        scene.Detach();
        Dispatcher.UIThread.RunJobs();
        Assert.False(state.IsRunning);
        Assert.Equal(40, state.Value, 7);
        state.UpdateTarget(200, Linear());
        Assert.Equal(0, scene.Frames.PendingFrameCount);
        scene.Right.Children.Add(host);
        scene.Step(0);
        Assert.Equal(40, state.Value, 7);
        scene.Step(50);
        Assert.Equal(120, state.Value, 7);
    });

    [Fact]
    public Task FixedScopeDisposal_EndsOnlyItsStatesAndRestoresProperties() => OnUi(() =>
    {
        var host = new Border { Opacity = .8 };
        using var scene = new ReparentFixture(host);
        var scope = AnimationScope.Get(host).CreateChild();
        using var state = AnimatedState.Create(scope, .2d);
        using var other = AnimatedState.Create(AnimationScope.GetRoot(scene.Window), 0d);
        using var connection = state.Bind(host, Visual.OpacityProperty);
        state.UpdateTarget(.6, Linear());
        other.UpdateTarget(100, Linear());
        scene.Step(0);
        scope.Dispose();
        Assert.Equal(.8, host.Opacity);
        Assert.False(state.IsRunning);
        Assert.Throws<ObjectDisposedException>(() => state.UpdateTarget(.4));
        scene.Step(100);
        Assert.Equal(100, other.Value);
    });

    [Fact]
    public Task DisposeInValueNotification_PreventsFinishedAndResurrection() => OnUi(() =>
    {
        var host = new Border { Opacity = .8 };
        using var scene = new ReparentFixture(host);
        var state = AnimatedState.Create(host, 0d);
        using var connection = state.Bind(host, Visual.OpacityProperty);
        var finished = 0;
        state.Finished += _ => finished++;
        state.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(state.Value)) state.Dispose(); };
        state.UpdateTarget(1, Linear());
        scene.Step(0);
        scene.Step(100);
        Assert.Equal(0, finished);
        Assert.Equal(.8, host.Opacity);
        scene.Move();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(.8, host.Opacity);
        Assert.Equal(0, scene.Frames.PendingFrameCount);
    });

    [Fact]
    public Task DisposeDuringBindingInstallation_RemovesReturnedContribution() => OnUi(() =>
    {
        var host = new Border { Opacity = .8 };
        using var scene = new ReparentFixture(host);
        using var state = AnimatedState.Create(host, .2d);
        host.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.OpacityProperty && host.Opacity == .2) state.Dispose();
        };
        using var connection = state.Bind(host, Visual.OpacityProperty);
        Assert.Equal(.8, host.Opacity);
        Assert.Equal(0, AnimationScope.Get(host).Diagnostics.BindingCount);
    });

    [Fact]
    public Task FailingSpec_ReportsOriginalError_AndDoesNotRetryEachLayout() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d);
        var failure = new InvalidOperationException("bad spec");
        var spec = new CountingSpec(failure);
        var errors = new List<Exception>();
        window.AnimationRoot.AnimationFailed += (_, e) => errors.Add(e.Exception);
        state.UpdateTarget(100, spec);
        clock.Pump(Ms(0));
        Assert.Same(failure, Assert.Single(errors));
        Assert.Equal(0, state.Value);
        Assert.False(state.IsRunning);
        for (var i = 0; i < 20; i++) state.UpdateTarget(100, spec);
        Assert.Equal(1, spec.Calls);
        Assert.Equal(0, clock.PendingFrameCount);
        state.UpdateTarget(100, Linear());
        clock.Pump(Ms(0));
        clock.Pump(Ms(100));
        Assert.Equal(100, state.Value);
        window.Close();
    });

    [Fact]
    public Task MeasureUpdates_HaveNoSynchronousNotifications_AndOneCommit() => OnUi(() =>
    {
        var probe = new MeasureProbe();
        using var scene = new ReparentFixture(probe);
        probe.Record = true;
        probe.Measure(new Size(100, 40));
        probe.Measure(new Size(120, 40));
        probe.Measure(new Size(80, 40));
        Assert.Equal(0, probe.Spec.Calls);
        Assert.False(probe.NotifiedInMeasure);
        scene.Step(0);
        Assert.Equal(1, probe.Spec.Calls);
        Assert.Equal(80, probe.State.TargetValue);
        Assert.False(probe.NotifiedInMeasure);
        probe.State.Dispose();
    });

    [Fact]
    public Task CrossRootMove_DoesNotReuseOldTimestampOrigin() => OnUi(() =>
    {
        var host = new Border { Opacity = .9 };
        using var left = new ReparentFixture(host);
        using var right = new ReparentFixture(new Border());
        right.Frames.AdvanceBy(Ms(5000));
        using var state = AnimatedState.Create(host, 0d);
        using var connection = state.Bind(host, Visual.OpacityProperty);
        state.UpdateTarget(1, Linear());
        left.Step(0);
        left.Step(40);
        left.Left.Children.Remove(host);
        right.Right.Children.Add(host);
        Dispatcher.UIThread.RunJobs();
        right.Step(0);
        Assert.Equal(.4, state.Value, 7);
        Assert.Equal(.4, host.Opacity, 7);
        right.Step(50);
        Assert.Equal(.7, state.Value, 7);
    });

    [Fact]
    public Task BindingFallbackUsesLatestBase_AndInheritedPropertiesAreSupported() => OnUi(() =>
    {
        var owner = new Border();
        using var scene = new ReparentFixture(owner);
        var text = new TextBlock { FontSize = 12 };
        scene.Right.Children.Add(text);
        using var state = AnimatedState.Create(owner, 20d);
        using var connection = state.Bind(text, TextBlock.FontSizeProperty);
        Assert.Equal(20, text.FontSize);
        text.FontSize = 16;
        Assert.Equal(20, text.FontSize);
        connection.Dispose();
        Assert.Equal(16, text.FontSize);
    });

    [Fact]
    public Task FirstContributionFailure_IsIsolatedAndLeavesNoBindingBehind() => OnUi(() =>
    {
        var owner = new Border();
        using var scene = new ReparentFixture(owner);
        var broken = new Border { Opacity = .8 };
        var good = new Border { Opacity = .9 };
        scene.Right.Children.Add(broken);
        scene.Right.Children.Add(good);
        using var state = AnimatedState.Create(owner, .2d);
        var failure = new InvalidOperationException("property handler");
        var errors = new List<Exception>();
        AnimationScope.GetRoot(scene.Window).AnimationFailed += (_, e) => errors.Add(e.Exception);
        broken.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.OpacityProperty && broken.Opacity == .2) throw failure;
        };
        using var badConnection = state.Bind(broken, Visual.OpacityProperty);
        using var goodConnection = state.Bind(good, Visual.OpacityProperty);
        Assert.Same(failure, Assert.Single(errors));
        Assert.Equal(.8, broken.Opacity);
        state.UpdateTarget(.6, Linear());
        scene.Step(0);
        scene.Step(100);
        Assert.Equal(.6, good.Opacity);
        Assert.Equal(.8, broken.Opacity);
        Assert.Single(errors);
    });

    [Fact]
    public Task CallbackDispose_StopsRemainingMulticastHandlers() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d);
        var staleCalls = 0;
        state.ValueChanged += _ => state.Dispose();
        state.ValueChanged += _ => staleCalls++;
        state.UpdateTarget(100, Linear());
        clock.Pump(Ms(0));
        clock.Pump(Ms(100));
        Assert.Equal(0, staleCalls);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task ValueCallbackFailure_DoesNotDiscardANewerRequestedTarget() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d);
        var errors = new List<Exception>();
        window.AnimationRoot.AnimationFailed += (_, e) => errors.Add(e.Exception);
        Action<double>? handler = null;
        handler = _ =>
        {
            state.ValueChanged -= handler;
            state.UpdateTarget(200, Linear());
            throw new InvalidOperationException("notification");
        };
        state.ValueChanged += handler;
        state.UpdateTarget(100, Linear());
        clock.Pump(Ms(0));
        clock.Pump(Ms(50));
        Assert.Single(errors);
        clock.Pump(Ms(0));
        clock.Pump(Ms(100));
        Assert.Equal(200, state.Value);
        window.Close();
    });

    [Fact]
    public Task ExplicitZeroThreshold_IsRejectedRatherThanTreatedAsMissing() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d, visibilityThreshold: 0d);
        var errors = new List<Exception>();
        window.AnimationRoot.AnimationFailed += (_, e) => errors.Add(e.Exception);
        state.UpdateTarget(100);
        clock.Pump(Ms(0));
        Assert.IsType<ArgumentOutOfRangeException>(Assert.Single(errors));
        Assert.Equal(0, state.Value);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SpringThresholdAndRetarget_MatchUnderlyingRuntime(bool thresholdOverride) => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var supplied = new SpringSpec<double>(.8, 200, .2);
        var expectedSpec = thresholdOverride ? new SpringSpec<double>(.8, 200, .03) : supplied;
        using var state = AnimatedState.Create(window.AnimationRoot, 0d,
            visibilityThreshold: thresholdOverride ? new Optional<double>(.03) : default);
        var reference = Animatable.Create(0d);
        state.UpdateTarget(100, supplied);
        var run = reference.AnimateToAsync(window.AnimationRoot, 100, expectedSpec);
        clock.Pump(Ms(0));
        for (var i = 0; i < 3; i++)
        {
            clock.Pump(Ms(16));
            Assert.Equal(reference.Value, state.Value, 7);
        }
        // Submit the reference's replacement at the same sampling boundary as AnimatedState.
        Task<AnimationResult<double>>? replacement = null;
        using var hook = window.AnimationRoot.FrameClock.RegisterBeforeFrame(() =>
        {
            replacement ??= reference.AnimateToAsync(window.AnimationRoot, 0, expectedSpec);
        });
        state.UpdateTarget(0, supplied);
        for (var i = 0; i < 5; i++)
        {
            clock.Pump(Ms(16));
            Assert.Equal(reference.Value, state.Value, 7);
        }
        reference.Stop();
        window.Close();
    });

    private sealed class CountingSpec(Exception? error = null) : IAnimationSpec<double>
    {
        public int Calls { get; private set; }
        public IVectorizedAnimationSpec Vectorize(ITwoWayConverter<double> converter)
        {
            Calls++;
            if (error is not null) throw error;
            return Linear().Vectorize(converter);
        }
    }

    [Fact]
    public Task CanceledFrameSource_ReportsOnceWithoutSpinningDemandLoop() => OnUi(() =>
    {
        var clock = new CanceledFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d);
        var failures = 0;
        window.AnimationRoot.AnimationFailed += (_, _) => failures++;
        state.UpdateTarget(100, Linear());
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clock.Requests);
        Assert.Equal(1, failures);
        Assert.False(state.IsRunning);
        state.UpdateTarget(200, Linear());
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clock.Requests);
        Assert.Equal(1, failures);
        window.Close();
    });

    [Fact]
    public Task ForeignHookFailure_DoesNotPermanentlyDisableCommitCoordinator() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var failure = new InvalidOperationException("before-frame hook");
        var hook = window.AnimationRoot.FrameClock.RegisterBeforeFrame(() => throw failure);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d);
        var errors = new List<Exception>();
        window.AnimationRoot.AnimationFailed += (_, e) => errors.Add(e.Exception);
        state.UpdateTarget(100, Linear());
        clock.Pump(Ms(0));
        Assert.Same(failure, Assert.Single(errors));
        Assert.Equal(0, clock.PendingFrameCount);
        hook.Dispose();
        state.UpdateTarget(200, Linear());
        clock.Pump(Ms(0));
        clock.Pump(Ms(100));
        Assert.Equal(200, state.Value);
        window.Close();
    });

    private sealed class CanceledFrameSource : IFrameSource
    {
        public int Requests { get; private set; }
        public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
        {
            Requests++;
            return ValueTask.FromCanceled<TimeSpan>(new CancellationToken(true));
        }
    }

    [Fact]
    public Task DisposalDuringSpecEvaluation_CannotSetRunningAgain() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var state = AnimatedState.Create(window.AnimationRoot, 0d);
        state.UpdateTarget(100, new CallbackSpec(state.Dispose));
        clock.Pump(Ms(0));
        Assert.False(state.IsRunning);
        Assert.Equal(0, state.Value);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    private sealed class CallbackSpec(Action callback) : IAnimationSpec<double>
    {
        public IVectorizedAnimationSpec Vectorize(ITwoWayConverter<double> converter)
        {
            callback();
            return Linear().Vectorize(converter);
        }
    }

    private sealed class MeasureProbe : Control
    {
        private bool _measuring;
        public bool Record { get; set; }
        public bool NotifiedInMeasure { get; private set; }
        public CountingSpec Spec { get; } = new();
        public AnimatedState<double> State { get; }
        public MeasureProbe()
        {
            State = AnimatedState.Create(this, 0d);
            State.PropertyChanged += (_, _) => NotifiedInMeasure |= _measuring;
            State.ValueChanged += _ => { NotifiedInMeasure |= _measuring; InvalidateMeasure(); };
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            _measuring = true;
            try { if (Record) State.UpdateTarget(availableSize.Width, Spec); return new Size(25, 25); }
            finally { _measuring = false; }
        }
    }
}
