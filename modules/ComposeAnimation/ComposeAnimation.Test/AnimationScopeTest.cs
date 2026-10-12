using System.ComponentModel;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimationScopeTest
{
    private static Task OnUi(Action action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None);
    private static Task OnUi(Func<Task> action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None);
    private static TweenSpec<double> Tween() => new(TimeSpan.FromMilliseconds(100), easing: new LinearEasing());
    private static void Send(ManualFrameSource clock, int ms)
    {
        clock.AdvanceBy(TimeSpan.FromMilliseconds(ms));
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public Task Initialization_IsRequiredAndCannotBeReplaced() => OnUi(() =>
    {
        var window = new Window();
        Assert.Throws<InvalidOperationException>(() => AnimationScope.Get(window));
        var root = AnimationScope.Initialize(window, new ManualFrameSource());
        Assert.Throws<InvalidOperationException>(() => AnimationScope.Initialize(window));
        // Get-owned: one scope per control, returned again on every call, parented to the root.
        var a = AnimationScope.Get(window);
        var b = AnimationScope.Get(window);
        Assert.Same(a, b);
        Assert.Same(root, a.Diagnostics.Parent);
        window.Close();
    });

    [Fact]
    public Task ScopeObjects_ArePermanentlyInvalid_ButFinalValueIsReadable() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild();
        var value = scope.CreateAnimatable(0d);
        var transition = scope.CreateTransition(false);
        var child = transition.AnimateDouble(state => state ? 1 : 0);
        var infinite = scope.CreateInfiniteTransition();
        var playing = value.AnimateToAsync(1, Tween());
        Send(clock, 0); Send(clock, 25);
        var final = value.Value;
        scope.Dispose();
        Assert.Throws<ObjectDisposedException>(() => { _ = value.AnimateToAsync(2); });
        Assert.Throws<ObjectDisposedException>(() => { _ = value.SnapToAsync(2); });
        Assert.Throws<ObjectDisposedException>(() => value.UpdateBounds(default));
        Assert.Throws<ObjectDisposedException>(() => transition.UpdateTarget(true));
        Assert.Throws<ObjectDisposedException>(() => transition.AnimateDouble(_ => 0));
        Assert.Throws<ObjectDisposedException>(() => child.Update(_ => 2));
        Assert.Throws<ObjectDisposedException>(() => infinite.DurationScale = 2);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => playing);
        await scope.Diagnostics.EndAsync();
        Assert.Equal(final, value.Value);
        value.Stop(); transition.Dispose(); infinite.Dispose(); scope.Dispose();
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task QueuedSnap_CannotCommitAfterEndingFromACancellationNotification() => OnUi(async () =>
    {
        var window = new AnimationTestWindow(new ManualFrameSource());
        var scope = window.AnimationRoot.CreateChild();
        var value = scope.CreateAnimatable(0d);
        var playing = value.AnimateToAsync(1, Tween());
        value.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(value.IsRunning) && !value.IsRunning) scope.Dispose();
        };
        var snap = value.SnapToAsync(2);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => playing);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => snap);
        await scope.Diagnostics.EndAsync();
        Assert.Equal(0, value.Value);
        window.Close();
    });

    [Fact]
    public Task Launch_StartsInlineAndRegistersBeforeUserReentry() => OnUi(async () =>
    {
        var window = new AnimationTestWindow();
        var scope = window.AnimationRoot.CreateChild();
        var called = false;
        var task = scope.Launch(_ =>
        {
            called = true;
            Assert.Equal(1, scope.Diagnostics.ActiveTaskCount);
            scope.Dispose();
            return Task.CompletedTask;
        });
        Assert.True(called);
        Assert.True(task.IsCompletedSuccessfully);
        await scope.Diagnostics.EndAsync();
        Assert.Equal(0, scope.Diagnostics.ActiveTaskCount);
        Assert.Equal(0, window.AnimationRoot.Diagnostics.ActiveChildCount);
        window.Close();
    });

    [Fact]
    public Task FaultedCancellationException_IsReported_WhileCanceledTasksAreNot() => OnUi(async () =>
    {
        var window = new AnimationTestWindow();
        var root = window.AnimationRoot;
        var failures = new List<AnimationFailureEventArgs>();
        root.AnimationFailed += (_, failure) => failures.Add(failure);
        var error = new OperationCanceledException("fault, not cancellation");
        var faulted = root.Launch(_ => Task.FromException(error), "Faulted task");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => faulted);
        Assert.True(faulted.IsFaulted);
        var canceled = root.Launch(_ => Task.FromCanceled(new CancellationToken(true)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.True(canceled.IsCanceled);
        Assert.Same(error, Assert.Single(failures).Exception);
        Assert.Equal("Faulted task", failures[0].OperationLabel);
        window.Close();
    });

    [Fact]
    public Task PropagatedFailures_AreDeduplicated_AndIndependentFailuresRemainVisible() => OnUi(async () =>
    {
        var window = new AnimationTestWindow();
        var root = window.AnimationRoot;
        var child = root.CreateChild("Child");
        var failures = new List<AnimationFailureEventArgs>();
        root.AnimationFailed += (_, failure) => failures.Add(failure);
        var first = new InvalidOperationException("same message");
        var second = new ArgumentException("same message");
        var parent = root.Launch(async _ =>
        {
            try { await child.Launch(_ => throw first, "Child failure"); }
            catch { throw new AggregateException(first, second); }
        }, "Parent");
        await Assert.ThrowsAsync<AggregateException>(() => parent);
        Assert.Equal(2, failures.Count);
        Assert.Same(child, failures[0].SourceScope);
        Assert.NotEqual(failures[0].FailureId, failures[1].FailureId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => root.Launch(_ => throw first));
        Assert.Equal(3, failures.Count); // Reused exception in an independent operation.
        window.Close();
    });

    [Fact]
    public Task DisposeAsync_WaitsForFinally_AndRejectsSelfOrAncestorWaits() => OnUi(async () =>
    {
        var window = new AnimationTestWindow();
        var root = window.AnimationRoot;
        var scope = root.CreateChild();
        var release = new TaskCompletionSource();
        var cleanupEntered = new TaskCompletionSource();
        var task = scope.Launch(async ct =>
        {
            try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); }
            finally { cleanupEntered.SetResult(); await release.Task; }
        });
        var disposal = scope.Diagnostics.EndAsync();
        await cleanupEntered.Task;
        Assert.False(disposal.IsCompleted);
        Assert.Equal(1, root.Diagnostics.ActiveChildCount);
        release.SetResult();
        await disposal;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(0, root.Diagnostics.ActiveChildCount);
        await root.Launch(async _ =>
        {
            Assert.Throws<InvalidOperationException>(() => { var t = root.Diagnostics.EndAsync(); });
            using var child = root.CreateChild(); // A parent may wait for a child.
            await child.Launch(_ =>
            {
                Assert.Throws<InvalidOperationException>(() => { var t = root.Diagnostics.EndAsync(); });
                Assert.Throws<InvalidOperationException>(() => { var t = child.Diagnostics.EndAsync(); });
                return Task.CompletedTask;
            });
        });
        window.Close();
    });

    [Fact]
    public Task Consumers_DisableBeforeCleanup_AndOnlyRemoveTheirOwnContribution() => OnUi(async () =>
    {
        var window = new AnimationTestWindow();
        var scope = window.AnimationRoot.CreateChild();
        var source = new Sample { Value = .8 };
        var target = new Border { Opacity = .2 };
        var observed = new List<double>();
        scope.SubscribeValue(source, observed.Add);
        var binding = scope.BindAnimatedValue(target, Visual.OpacityProperty, source);
        Assert.Equal(.8, target.Opacity);
        source.Publish(null); source.Publish(""); source.Publish("Other");
        Assert.Equal(3, observed.Count);
        binding.Dispose();
        Assert.Equal(.2, target.Opacity);
        scope.BindAnimatedValue(target, Visual.OpacityProperty, source);
        target.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.OpacityProperty && target.Opacity == .2) source.Publish("Value");
        };
        await scope.Diagnostics.EndAsync();
        source.Publish("Value");
        Assert.Equal(3, observed.Count);
        Assert.Equal(.2, target.Opacity);
        Assert.Equal(0, scope.Diagnostics.SubscriptionCount);
        Assert.Equal(0, scope.Diagnostics.BindingCount);
        window.Close();
    });

    [Fact]
    public Task DiagnosticHandlerFaults_DoNotPreventOtherHandlersOrCleanup() => OnUi(async () =>
    {
        var window = new AnimationTestWindow();
        var root = window.AnimationRoot;
        var handlerError = new Exception("observer");
        root.AnimationFailed += (_, _) => throw handlerError;
        var count = 0;
        root.AnimationFailed += (_, _) => count++;
        var scope = root.CreateChild();
        scope.RegisterResource(() => throw new Exception("resource"));
        var released = false;
        scope.RegisterResource(() => released = true);
        await scope.Diagnostics.EndAsync();
        Assert.True(released);
        Assert.Equal(1, count);
        Assert.Same(handlerError, root.Diagnostics.LastDiagnosticError);
        Assert.Equal(0, root.Diagnostics.ActiveChildCount);
        window.Close();
    });

    [Fact]
    public Task ManyTemporaryEffects_ReturnAllRegistrationsToBaseline() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var root = window.AnimationRoot;
        for (var i = 0; i < 1500; i++)
        {
            using var ripple = root.CreateChild("Ripple");
            var alpha = ripple.CreateAnimatable(0d);
            ripple.SubscribeValue(alpha, _ => { });
            ripple.BindAnimatedValue(new Border(), Visual.OpacityProperty, alpha);
            await ripple.Launch(ct => alpha.SnapToAsync(1, ct));
        }
        Assert.Equal(0, root.Diagnostics.ActiveTaskCount);
        Assert.Equal(0, root.Diagnostics.ActiveChildCount);
        Assert.Equal(0, root.Diagnostics.PendingAnimationCount);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task DirectAndPropertyAnimations_ShareOneRootRequest_AndDetachIndependently() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var panel = new StackPanel();
        var a = new Border { Opacity = 1 };
        var b = new Border { Opacity = 1 };
        foreach (var visual in new[] { a, b })
        {
            visual.Animate(new AnimatedDouble { Property = Visual.OpacityProperty, Spec = Tween() });
            panel.Children.Add(visual);
        }
        var window = new AnimationTestWindow(clock) { Content = panel };
        window.Show(); Dispatcher.UIThread.RunJobs();
        var first = AnimationScope.Get(a);
        var second = AnimationScope.Get(b);
        var x = first.CreateAnimatable(0d);
        var y = second.CreateAnimatable(0d);
        var firstTask = first.Launch(ct => x.AnimateToAsync(1, Tween(), cancellationToken: ct));
        var secondTask = second.Launch(ct => y.AnimateToAsync(1, Tween(), cancellationToken: ct));
        a.Opacity = b.Opacity = 0;
        Assert.Equal(1, clock.PendingFrameCount);
        Send(clock, 0); Send(clock, 25);
        panel.Children.Remove(a);
        await first.Diagnostics.Ended; // Detach marks stale; the drain ends it.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstTask);
        Assert.Equal(1, clock.PendingFrameCount);
        Send(clock, 75);
        await secondTask;
        Assert.Equal(1, y.Value);
        Assert.Equal(0, b.Opacity);
        window.Close();
        await window.AnimationRoot.Diagnostics.EndAsync();
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Equal(0, window.AnimationRoot.Diagnostics.ActiveChildCount);
    });

    [Fact]
    public Task SnapToTarget_EndsSameTargetImmediately_AndPreservesDeclarationAndBaseBinding() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var visual = new Border { Opacity = 1 };
        var declaration = visual.Animate(new AnimatedDouble { Property = Visual.OpacityProperty, Spec = Tween() });
        var window = new AnimationTestWindow(clock) { Content = visual };
        window.Show(); Dispatcher.UIThread.RunJobs();
        visual.Opacity = 0;
        Send(clock, 0); Send(clock, 25);
        Assert.True(visual.IsAnimating(Visual.OpacityProperty));
        // The base destination changes under the animation contribution before another frame.
        visual.Opacity = .7;
        Animated.SnapToTargetAsync(visual, Visual.OpacityProperty);
        Assert.Equal(.7, visual.Opacity);
        Assert.False(visual.IsAnimating(Visual.OpacityProperty));
        Assert.Same(declaration, Animated.GetProperties(visual)![0]);
        Send(clock, 500);
        Assert.Equal(.7, visual.Opacity);
        visual.Opacity = .2;
        Assert.True(visual.IsAnimating(Visual.OpacityProperty));
        Animated.SnapToTargetAsync(visual, Visual.OpacityProperty);
        Assert.Equal(.2, visual.Opacity);
        window.Close();
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Popup_ResolvesWindowEnvironmentBeforeFirstControlScope(bool overlay) => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var panel = new StackPanel();
        var anchor = new Border { Width = 100, Height = 40 };
        panel.Children.Add(anchor);
        var window = new AnimationTestWindow(clock) { Content = panel };
        window.Show(); Dispatcher.UIThread.RunJobs();
        var content = new Border { Width = 80, Height = 50 };
        var popup = new Popup { PlacementTarget = anchor, Child = content, ShouldUseOverlayLayer = overlay };
        panel.Children.Add(popup);
        popup.IsOpen = true; Dispatcher.UIThread.RunJobs();
        var scope = AnimationScope.Get(content);
        var sample = scope.CreateAnimatable(0d);
        _ = scope.Launch(ct => sample.AnimateToAsync(1, Tween(), cancellationToken: ct));
        Assert.Equal(1, clock.PendingFrameCount);
        popup.IsOpen = false; Dispatcher.UIThread.RunJobs();
        Assert.True(scope.IsEnding);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public async Task ManualClock_IsMonotonicAndSendsExactlyOneFixedBatch()
    {
        using var clock = new ManualFrameSource();
        var first = clock.NextFrameAsync(default).AsTask();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        Assert.Equal(TimeSpan.FromMilliseconds(60), await first);
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.SendFrame(TimeSpan.Zero));
        var second = clock.NextFrameAsync(default).AsTask();
        clock.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [Fact]
    public Task RootEnding_RemovesPropertyContributionsAndCancelsAllWaitersWithoutFailure() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var visual = new Border { Opacity = 1 };
        visual.Animate(new AnimatedDouble { Property = Visual.OpacityProperty, Spec = Tween() });
        var window = new AnimationTestWindow(clock) { Content = visual };
        window.Show(); Dispatcher.UIThread.RunJobs();
        var errors = new List<Exception>();
        window.AnimationRoot.AnimationFailed += (_, e) => errors.Add(e.Exception);
        visual.Opacity = .2;
        Send(clock, 0); Send(clock, 25);
        Assert.InRange(visual.Opacity, .3, .99);
        await window.AnimationRoot.Diagnostics.EndAsync();
        Assert.Equal(.2, visual.Opacity);
        Assert.False(visual.IsAnimating(Visual.OpacityProperty));
        Assert.Empty(errors);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Equal(0, window.AnimationRoot.Diagnostics.ActiveChildCount);
        window.Close();
    });

    [Fact]
    public Task FrameSourceFailure_IsReportedOnceAndDoesNotRetry() => OnUi(async () =>
    {
        var clock = new FailingClock();
        var window = new AnimationTestWindow(clock);
        var root = window.AnimationRoot;
        var failures = new List<AnimationFailureEventArgs>();
        root.AnimationFailed += (_, e) => failures.Add(e);
        var a = root.CreateChild(); var b = root.CreateChild();
        var first = a.Launch(ct => a.CreateAnimatable(0d).AnimateToAsync(1, Tween(), cancellationToken: ct));
        var second = b.Launch(ct => b.CreateAnimatable(0d).AnimateToAsync(1, Tween(), cancellationToken: ct));
        Assert.Equal(1, clock.Requests);
        var error = new InvalidOperationException("frame source");
        clock.Pending.SetException(error);
        await Assert.ThrowsAsync<InvalidOperationException>(() => first);
        await Assert.ThrowsAsync<InvalidOperationException>(() => second);
        await Assert.ThrowsAsync<InvalidOperationException>(() => a.Launch(ct =>
            a.CreateAnimatable(0d).AnimateToAsync(2, cancellationToken: ct)));
        Assert.Same(root, Assert.Single(failures).SourceScope);
        Assert.Same(error, root.Diagnostics.LastError);
        Assert.Equal(1, clock.Requests);
        await root.Diagnostics.EndAsync(); window.Close();
    });

    [Fact]
    public Task PlaybackPolicy_KeepsFiniteProgressAndResumesInfiniteFromZero() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var root = window.AnimationRoot;
        var value = root.CreateAnimatable(0d);
        var task = value.AnimateToAsync(1, Tween());
        Send(clock, 0); Send(clock, 25);
        Assert.Equal(.25, value.Value, 7);
        root.DurationScale = 2;
        Send(clock, 50);
        Assert.Equal(.5, value.Value, 7);
        Send(clock, 100);
        await task;
        root.DurationScale = 0;
        var infinite = root.CreateInfiniteTransition();
        var repeat = infinite.AnimateDouble(0, 1, new InfiniteRepeatableSpec<double>(Tween()));
        var loop = root.Launch(ct => infinite.RunAsync(ct));
        await AnimationTestDispatcher.WaitUntilAsync(() => repeat.Value == 1 && clock.PendingFrameCount == 0);
        root.DurationScale = 1;
        await AnimationTestDispatcher.WaitUntilAsync(() => clock.PendingFrameCount == 1);
        Send(clock, 0); Send(clock, 25);
        Assert.Equal(.25, repeat.Value, 7);
        await root.Diagnostics.EndAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loop);
        window.Close();
    });

    [Fact]
    public Task BindingReentry_WhenInitialValueEndsScope_LeavesNoContribution() => OnUi(async () =>
    {
        var window = new AnimationTestWindow();
        var scope = window.AnimationRoot.CreateChild();
        var target = new Border { Opacity = .2 };
        target.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.OpacityProperty && target.Opacity == .8) scope.Dispose();
        };
        scope.BindAnimatedValue(target, Visual.OpacityProperty, new Sample { Value = .8 });
        await scope.Diagnostics.EndAsync();
        Assert.Equal(.2, target.Opacity);
        Assert.Equal(0, scope.Diagnostics.BindingCount);
        Assert.Equal(0, scope.Diagnostics.SubscriptionCount);
        window.Close();
    });

    [Fact]
    public Task BuiltInLoops_EndWithRootEvenWhileControlsRemainAttached() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var visible = new AnimatedVisibility { Content = new Border { Width = 40, Height = 40 } };
        var content = new AnimatedContent { Content = "first" };
        var sizeChild = new Border { Width = 40, Height = 40 };
        var size = new AnimatedContentSize { Content = sizeChild };
        var bounds = new AnimatedBounds { Child = new LookaheadTextBlock { Text = "Bounds" }, TargetSize = new Size(40, 40) };
        var approach = new LookaheadSize { Child = new LookaheadTextBlock { Text = "Approach" }, TargetSize = new Size(40, 40) };
        var panel = new StackPanel();
        panel.Children.Add(visible); panel.Children.Add(content); panel.Children.Add(size);
        panel.Children.Add(bounds); panel.Children.Add(approach);
        var window = new AnimationTestWindow(clock) { Content = panel, Width = 300, Height = 300 };
        window.Show(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        visible.Visible = false; content.Content = "second"; sizeChild.Height = 100;
        bounds.TargetSize = new Size(100, 60); approach.TargetSize = new Size(100, 60);
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        Send(clock, 0); Send(clock, 25); window.UpdateLayout();
        Assert.True(bounds.IsRunning);
        var failures = new List<Exception>();
        window.AnimationRoot.AnimationFailed += (_, e) => failures.Add(e.Exception);
        await window.AnimationRoot.Diagnostics.EndAsync();
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        Assert.Empty(failures);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Equal(0, window.AnimationRoot.Diagnostics.ActiveTaskCount);
        Assert.Equal(0, window.AnimationRoot.Diagnostics.ActiveChildCount);
        Assert.True(visible.IsIdle); Assert.True(content.IsIdle);
        Assert.False(bounds.IsRunning);
        window.Close();
    });

    [Fact]
    public Task TransitionAndInfiniteValues_BindCheckBoxProgressAndRotation() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild("Value consumers");
        var checkbox = new CheckBox { IsChecked = true, Opacity = .3 };
        var progress = new ProgressBar { Value = 10, Maximum = 100 };
        var rotation = new Avalonia.Media.RotateTransform { Angle = 15 };
        var transition = scope.CreateTransition(false);
        var amount = transition.AnimateDouble(state => state ? 100 : 0, _ => Tween());
        var alpha = transition.AnimateDouble(state => state ? 1 : 0, _ => Tween());
        scope.BindAnimatedValue(progress, ProgressBar.ValueProperty, amount);
        scope.BindAnimatedValue(checkbox, Visual.OpacityProperty, alpha);
        var running = scope.Launch(ct => transition.RunAsync(ct));
        transition.UpdateTarget(checkbox.IsChecked == true);
        var infinite = scope.CreateInfiniteTransition();
        var angle = infinite.AnimateDouble(0, 360, new InfiniteRepeatableSpec<double>(Tween()));
        scope.BindAnimatedValue(rotation, Avalonia.Media.RotateTransform.AngleProperty, angle);
        var looping = scope.Launch(ct => infinite.RunAsync(ct));
        Send(clock, 0); Send(clock, 50);
        Assert.Equal(50, progress.Value, 7); Assert.Equal(.5, checkbox.Opacity, 7);
        Assert.Equal(180, rotation.Angle, 7);
        await scope.Diagnostics.EndAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => looping);
        Assert.Equal(10, progress.Value); Assert.Equal(.3, checkbox.Opacity); Assert.Equal(15, rotation.Angle);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task NestedPopups_ReopenWithFreshScopesAndShareWindowFrames() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var panel = new StackPanel();
        var anchor = new Border { Width = 40, Height = 40 };
        panel.Children.Add(anchor);
        var window = new AnimationTestWindow(clock) { Content = panel };
        window.Show(); Dispatcher.UIThread.RunJobs();
        var outerContent = new StackPanel();
        var innerAnchor = new Border { Width = 20, Height = 20 };
        outerContent.Children.Add(innerAnchor);
        var outer = new Popup { PlacementTarget = anchor, Child = outerContent };
        panel.Children.Add(outer);
        outer.IsOpen = true; Dispatcher.UIThread.RunJobs();
        var innerContent = new Border { Width = 20, Height = 20 };
        var inner = new Popup { PlacementTarget = innerAnchor, Child = innerContent };
        outerContent.Children.Add(inner);
        inner.IsOpen = true; Dispatcher.UIThread.RunJobs();
        var first = AnimationScope.Get(outerContent);
        var nested = AnimationScope.Get(innerContent);
        var a = first.Launch(ct => first.CreateAnimatable(0d).AnimateToAsync(1, Tween(), cancellationToken: ct));
        var b = nested.Launch(ct => nested.CreateAnimatable(0d).AnimateToAsync(1, Tween(), cancellationToken: ct));
        Assert.Equal(1, clock.PendingFrameCount);
        inner.IsOpen = false; outer.IsOpen = false; Dispatcher.UIThread.RunJobs();
        await first.Diagnostics.Ended; await nested.Diagnostics.Ended;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => a);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b);
        outer.IsOpen = true; Dispatcher.UIThread.RunJobs();
        var reopened = AnimationScope.Get(outerContent);
        Assert.NotSame(first, reopened);
        Assert.False(reopened.IsEnding);
        var task = reopened.Launch(ct => reopened.CreateAnimatable(0d).AnimateToAsync(1, Tween(), cancellationToken: ct));
        Send(clock, 0); Send(clock, 100); await task;
        outer.IsOpen = false; window.Close();
        await window.AnimationRoot.Diagnostics.EndAsync();
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Equal(0, window.AnimationRoot.Diagnostics.ActiveChildCount);
    });

    [Fact]
    public Task PreviouslyLaunchedChildFailure_IsNotReportedAgainWhenAwaitedByManagedParent() => OnUi(async () =>
    {
        var window = new AnimationTestWindow();
        var root = window.AnimationRoot;
        var child = root.CreateChild();
        var failures = new List<AnimationFailureEventArgs>();
        root.AnimationFailed += (_, e) => failures.Add(e);
        var error = new InvalidOperationException("child");
        var task = child.Launch(async _ => { await Task.Yield(); throw error; });
        var parent = root.Launch(async _ => await task);
        await Assert.ThrowsAsync<InvalidOperationException>(() => parent);
        Assert.Single(failures);
        await Assert.ThrowsAsync<InvalidOperationException>(() => root.Launch(_ => throw error));
        Assert.Equal(2, failures.Count);
        window.Close();
    });

    private sealed class FailingClock : IFrameSource
    {
        internal TaskCompletionSource<TimeSpan> Pending { get; } = new();
        internal int Requests;
        public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
        {
            Requests++;
            return new ValueTask<TimeSpan>(Pending.Task.WaitAsync(cancellationToken));
        }
    }

    private sealed class Sample : IAnimationValue<double>
    {
        public double Value { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void Publish(string? property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}
