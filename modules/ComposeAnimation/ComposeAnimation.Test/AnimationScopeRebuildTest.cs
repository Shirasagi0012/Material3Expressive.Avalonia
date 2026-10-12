using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public sealed class AnimationScopeRebuildTest
{
    private static Task OnUi(Action action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None);
    private static Task OnUi(Func<Task> action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None);
    private static TweenSpec<double> Tween() => new(TimeSpan.FromMilliseconds(100), easing: new LinearEasing());

    private static AnimationTestWindow Show(Control content, ManualFrameSource? clock = null)
    {
        var window = new AnimationTestWindow(clock ?? new ManualFrameSource()) { Content = content, Width = 300, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static List<AnimationScopeChangedEventArgs> Record(Visual control)
    {
        var events = new List<AnimationScopeChangedEventArgs>();
        AnimationScope.AddScopeChangedHandler(control, (_, e) => events.Add(e));
        return events;
    }

    [Fact]
    public Task Get_ReturnsSameInstance_ThrowsWhenDetached_AndFirstCreationRaisesNothing() => OnUi(() =>
    {
        var control = new Border();
        Assert.Throws<InvalidOperationException>(() => AnimationScope.Get(control));
        var events = Record(control);
        var window = Show(new StackPanel { Children = { control } });
        var scope = AnimationScope.Get(control);
        Assert.Same(scope, AnimationScope.Get(control));
        Assert.Same(control, scope.Host);
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(events);
        window.Close();
    });

    [Fact]
    public Task NestedControl_ParentsToNearestAncestorScope_OrFallsBackToRoot() => OnUi(() =>
    {
        var leaf = new Border();
        var plain = new Border { Child = new Border { Child = leaf } };
        var other = new Border();
        var ancestor = new Decorator { Child = new Border { Child = other } };
        var window = Show(new StackPanel { Children = { plain, ancestor } });
        Assert.Same(window.AnimationRoot, AnimationScope.Get(leaf).Diagnostics.Parent);
        var ancestorScope = AnimationScope.Get(ancestor);
        var nested = AnimationScope.Get(other);
        Assert.Same(ancestorScope, nested.Diagnostics.Parent);
        window.Close();
    });

    [Fact]
    public Task ReparentBetweenScopes_EndsOldScope_ReleasesBeforeCancel_AndRaisesOnce() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var control = new Border();
        var left = new Decorator { Child = control };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } }, clock);
        var leftScope = AnimationScope.Get(left);
        var rightScope = AnimationScope.Get(right);
        var old = AnimationScope.Get(control);
        Assert.Same(leftScope, old.Diagnostics.Parent);
        var order = new List<string>();
        old.RegisterResource(() => order.Add(old.Lifetime.IsCancellationRequested ? "released-after-cancel" : "released"));
        var run = old.Launch(async ct =>
        {
            try { await Task.Delay(Timeout.Infinite, ct); }
            finally { order.Add("canceled"); }
        });
        var events = Record(control);

        left.Child = null;
        right.Child = control;
        Assert.Same(old, AnimationScope.Get(control)); // marked only; never rebuilt inside Get
        Assert.True(old.Diagnostics.IsStale);
        Dispatcher.UIThread.RunJobs();

        var change = Assert.Single(events);
        Assert.Same(old, change.OldScope);
        Assert.Same(AnimationScope.Get(control), change.NewScope);
        Assert.Same(rightScope, change.NewScope!.Diagnostics.Parent);
        Assert.Equal(1, change.NewScope.Diagnostics.Generation);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(["released", "canceled"], order);
        Assert.True(old.Diagnostics.IsEnding);
        window.Close();
    });

    [Fact]
    public Task ReparentWithinSameAncestorScope_KeepsScope_AndClearsStaleMark() => OnUi(() =>
    {
        var control = new Border();
        var a = new StackPanel { Children = { control } };
        var b = new StackPanel();
        var host = new Decorator { Child = new StackPanel { Children = { a, b } } };
        var window = Show(host);
        AnimationScope.Get(host);
        var scope = AnimationScope.Get(control);
        var events = Record(control);
        a.Children.Remove(control);
        b.Children.Add(control);
        Assert.True(scope.Diagnostics.IsStale);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(scope, AnimationScope.Get(control));
        Assert.False(scope.Diagnostics.IsStale);
        Assert.False(scope.Diagnostics.IsEnding);
        Assert.Empty(events);
        window.Close();
    });

    [Fact]
    public Task PanelRemoveAndReaddInOneTurn_DoesNotRebuildWhenParentIsUnchanged() => OnUi(() =>
    {
        var control = new Border();
        var panel = new StackPanel { Children = { control } };
        var window = Show(panel);
        var scope = AnimationScope.Get(control);
        var events = Record(control);
        panel.Children.Remove(control);
        panel.Children.Add(control);
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(events);
        Assert.Same(scope, AnimationScope.Get(control));
        Assert.Equal(0, scope.Diagnostics.Generation);
        window.Close();
    });

    [Fact]
    public Task ContentPresenterSwap_RebuildsMovedControlUnderNewParentOnce() => OnUi(() =>
    {
        var control = new Border();
        var first = new ContentControl { Content = control };
        var second = new ContentControl();
        var window = Show(new StackPanel { Children = { first, second } });
        var secondScope = AnimationScope.Get(second);
        var old = AnimationScope.Get(control);
        var events = Record(control);
        first.Content = null;
        second.Content = control;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        var change = Assert.Single(events);
        Assert.Same(old, change.OldScope);
        Assert.Same(secondScope, change.NewScope!.Diagnostics.Parent);
        window.Close();
    });

    [Fact]
    public Task DetachWithoutReattach_EndsScope_ClearsLocalValue_AndRaisesNullNewScope() => OnUi(() =>
    {
        var control = new Border();
        var panel = new StackPanel { Children = { control } };
        var window = Show(panel);
        var old = AnimationScope.Get(control);
        var events = Record(control);
        panel.Children.Remove(control);
        Dispatcher.UIThread.RunJobs();
        var change = Assert.Single(events);
        Assert.Same(old, change.OldScope);
        Assert.Null(change.NewScope);
        Assert.True(old.Diagnostics.IsEnding);
        Assert.Null(control.GetValue(AnimationScope.ScopeProperty));
        window.Close();
    });

    [Fact]
    public Task AncestorRebuild_RebuildsGetDescendants_AndEndsChildScopes() => OnUi(() =>
    {
        var leaf = new Border();
        var ancestor = new Decorator { Child = leaf };
        var left = new Decorator { Child = ancestor };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } });
        AnimationScope.Get(left); var rightScope = AnimationScope.Get(right);
        var oldAncestor = AnimationScope.Get(ancestor);
        var oldLeaf = AnimationScope.Get(leaf);
        var child = oldAncestor.CreateChild();
        var leafEvents = Record(leaf);
        left.Child = null;
        right.Child = ancestor;
        Dispatcher.UIThread.RunJobs();
        var newAncestor = AnimationScope.Get(ancestor);
        Assert.Same(rightScope, newAncestor.Diagnostics.Parent);
        Assert.True(child.Diagnostics.IsEnding);
        var change = Assert.Single(leafEvents);
        Assert.Same(oldLeaf, change.OldScope);
        Assert.Same(newAncestor, change.NewScope!.Diagnostics.Parent);
        window.Close();
    });

    [Fact]
    public Task ManyMarksInOneTurn_CoalesceIntoOneDrainAndOneRebuildPerHost() => OnUi(() =>
    {
        var controls = Enumerable.Range(0, 20).Select(_ => new Border()).ToArray();
        var left = new StackPanel(); foreach (var c in controls) left.Children.Add(c);
        var leftHost = new Decorator { Child = left };
        var right = new StackPanel(); var rightHost = new Decorator { Child = right };
        var window = Show(new StackPanel { Children = { leftHost, rightHost } });
        AnimationScope.Get(leftHost); AnimationScope.Get(rightHost);
        var counts = controls.Select(c => { var list = Record(c); AnimationScope.Get(c); return list; }).ToArray();
        foreach (var c in controls) { left.Children.Remove(c); right.Children.Add(c); left.Children.Add(new Border()); }
        Assert.Equal(controls.Length, AnimationScope.Get(controls[0]).Diagnostics.WindowStaleCount);
        Dispatcher.UIThread.RunJobs();
        Assert.All(counts, list => Assert.Single(list));
        Assert.Equal(0, AnimationScope.Get(controls[0]).Diagnostics.WindowStaleCount);
        window.Close();
    });

    [Fact]
    public Task RestartInScopeChangedHandler_DuringFrameDrain_SamplesAtThatFramesTimestamp() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var control = new Border();
        var left = new Decorator { Child = control };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } }, clock);
        AnimationScope.Get(left); AnimationScope.Get(right);
        var keepAlive = window.AnimationRoot.CreateAnimatable(0d);
        _ = keepAlive.AnimateToAsync(1, new TweenSpec<double>(TimeSpan.FromSeconds(10), easing: new LinearEasing()));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Dispatcher.UIThread.RunJobs(FrameClock.ResumePriority);
        var restartedAt = new List<TimeSpan>();
        AnimationScope.AddScopeChangedHandler(control, (_, e) =>
        {
            if (e.NewScope is { } scope)
                _ = scope.Launch(async ct => restartedAt.Add(await scope.NextFrameAsync(ct)));
        });
        AnimationScope.Get(control);
        // Mark during the frame wait, then deliver the frame before the fallback post can run:
        // the before-frame hook drains and the restarted run joins this frame.
        left.Child = null; right.Child = control;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Dispatcher.UIThread.RunJobs(FrameClock.ResumePriority);
        Assert.Equal([TimeSpan.FromMilliseconds(32)], restartedAt);
        window.Close();
    });

    [Fact]
    public Task ReparentFromWaiterContinuation_TakesEffectAfterTheBatch() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var control = new Border();
        var left = new Decorator { Child = control };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } }, clock);
        AnimationScope.Get(left); AnimationScope.Get(right);
        var old = AnimationScope.Get(control);
        var stillOldDuringBatch = false;
        _ = window.AnimationRoot.Launch(async ct =>
        {
            await window.AnimationRoot.NextFrameAsync(ct);
            left.Child = null; right.Child = control;
            stillOldDuringBatch = ReferenceEquals(old, AnimationScope.Get(control)) && !old.Diagnostics.IsEnding;
        });
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Dispatcher.UIThread.RunJobs(FrameClock.ResumePriority);
        Assert.True(stillOldDuringBatch);
        Assert.NotSame(old, AnimationScope.Get(control));
        window.Close();
    });

    [Fact]
    public Task HandlerThatMakesItsHostStaleAgain_IsRebuiltNextFrameNotInALoop() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var control = new Border();
        var left = new Decorator { Child = control };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } }, clock);
        AnimationScope.Get(left); AnimationScope.Get(right);
        AnimationScope.Get(control);
        var changes = 0;
        AnimationScope.AddScopeChangedHandler(control, (_, e) =>
        {
            changes++;
            if (changes == 1) { right.Child = null; left.Child = control; }
        });
        left.Child = null; right.Child = control;
        Dispatcher.UIThread.RunJobs(); // post drain: one rebuild; the move back is deferred
        Assert.Equal(1, changes);
        Assert.True(AnimationScope.Get(control).Diagnostics.IsStale);
        Assert.Equal(1, clock.PendingFrameCount); // the drain requested the frame it deferred to
        _ = window.AnimationRoot.NextFrameAsync();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, changes);
        window.Close();
    });

    [Fact]
    public Task IdleDrain_ReturnsImmediately_WithoutAllocating() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var window = Show(new Border(), clock);
        var root = window.AnimationRoot;
        var waits = new List<Task>();
        for (var i = 0; i < 3; i++)
        {
            waits.Add(root.NextFrameAsync().AsTask());
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Dispatcher.UIThread.RunJobs();
        }
        Assert.Equal(0, root.Diagnostics.WindowStaleCount);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var drain = typeof(AnimationScope).GetMethod("DrainStale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .CreateDelegate<Action>(root);
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++) drain();
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        window.Close();
    });

    [Fact]
    public Task FallbackPost_DrainsWithoutFrames_AndNoOpsWhenTheHookWon() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var control = new Border();
        var panel = new StackPanel { Children = { control } };
        var window = Show(panel, clock);
        var scope = AnimationScope.Get(control);
        panel.Children.Remove(control);
        Assert.Equal(0, clock.PendingFrameCount);
        Dispatcher.UIThread.RunJobs();
        Assert.True(scope.Diagnostics.IsEnding); // no frame was needed

        var second = new Border();
        panel.Children.Add(second);
        var other = AnimationScope.Get(second);
        var waiter = window.AnimationRoot.NextFrameAsync().AsTask();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); // frame hop queued first
        panel.Children.Remove(second);                  // fallback post queued behind it
        var events = Record(second);
        Dispatcher.UIThread.RunJobs();
        Assert.Single(events);
        Assert.True(other.Diagnostics.IsEnding);
        Assert.True(waiter.IsCompletedSuccessfully);
        window.Close();
    });

    [Fact]
    public Task FlyoutContent_ParentsToTheOwnerControlsScope() => OnUi(() =>
    {
        var content = new Border { Width = 20, Height = 20 };
        var owner = new Button { Content = "owner", Flyout = new Flyout { Content = content } };
        var window = Show(new StackPanel { Children = { owner } });
        var ownerScope = AnimationScope.Get(owner);
        owner.Flyout!.ShowAt(owner);
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(TopLevel.GetTopLevel(content));
        Assert.Same(ownerScope, AnimationScope.Get(content).Diagnostics.Parent);
        owner.Flyout.Hide();
        window.Close();
    });

    [Fact]
    public Task StandalonePopup_FallsBackToItsTopLevelRoot_AndSharesTheWindowFrames() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var anchor = new Border { Width = 40, Height = 40 };
        var panel = new StackPanel { Children = { anchor } };
        var window = Show(panel, clock);
        var content = new Border { Width = 20, Height = 20 };
        var popup = new Popup { PlacementTarget = anchor, Child = content };
        panel.Children.Add(popup);
        popup.IsOpen = true; Dispatcher.UIThread.RunJobs();
        var scope = AnimationScope.Get(content);
        // No ancestor scope: falls back to the content's TopLevel root. The headless platform hosts
        // popups in the window's overlay layer, so that TopLevel is the window itself. The PopupRoot
        // branch of Resolve (a popup root parented to the window root) is not reachable headless.
        Assert.Same(window, TopLevel.GetTopLevel(content));
        Assert.Same(window.AnimationRoot, scope.Diagnostics.Parent);
        _ = scope.NextFrameAsync();
        _ = window.AnimationRoot.NextFrameAsync();
        Assert.Equal(1, clock.PendingFrameCount);
        popup.IsOpen = false; window.Close();
    });

    [Fact]
    public Task WindowClose_EndsEveryScopeIncludingPopups() => OnUi(() =>
    {
        var anchor = new Border { Width = 40, Height = 40 };
        var control = new Border();
        var panel = new StackPanel { Children = { anchor, control } };
        var window = Show(panel);
        var content = new Border();
        var popup = new Popup { PlacementTarget = anchor, Child = content };
        panel.Children.Add(popup); popup.IsOpen = true; Dispatcher.UIThread.RunJobs();
        var scopes = new[] { AnimationScope.Get(control), AnimationScope.Get(content), window.AnimationRoot };
        var child = scopes[0].CreateChild();
        window.Close(); Dispatcher.UIThread.RunJobs();
        Assert.All(scopes, s => Assert.True(s.Diagnostics.IsEnding));
        Assert.True(child.Diagnostics.IsEnding);
    });

    [Fact]
    public Task CreateChild_DisposesOnlyItself_EndsWithParent_AndIsIdempotent() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var control = new Border();
        var left = new Decorator { Child = control };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } }, clock);
        AnimationScope.Get(left); AnimationScope.Get(right);
        var parent = AnimationScope.Get(control);
        var parentRun = parent.Launch(ct => Task.Delay(Timeout.Infinite, ct));
        Task childRun;
        using (var child = parent.CreateChild("ripple"))
        {
            Assert.Same(parent.Host, child.Host);
            Assert.Null(control.GetValue(AnimationScope.ScopeProperty) is AnimationChildScope ? 1 : (int?)null);
            childRun = child.Launch(ct => Task.Delay(Timeout.Infinite, ct));
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => childRun);
        Assert.False(parentRun.IsCompleted);

        var survivor = parent.CreateChild();
        left.Child = null; right.Child = control; Dispatcher.UIThread.RunJobs();
        Assert.True(survivor.Diagnostics.IsEnding);
        survivor.Dispose(); survivor.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => parentRun);
        window.Close();
    });

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public Task NextFrameAsync_ResumesInlineInTheBatch_AndCancelsWithTheScope(int start) => OnUi(() =>
    {
        var priority = start switch { 0 => DispatcherPriority.Normal, 1 => DispatcherPriority.Background, _ => DispatcherPriority.Loaded };
        var clock = new ManualFrameSource();
        var window = Show(new Border(), clock);
        var root = window.AnimationRoot;
        var log = new List<string>();
        var child = root.CreateChild();
        Task? waiting = null;
        Task? canceled = null;
        Dispatcher.UIThread.Post(() =>
        {
            waiting = Wait();
            canceled = child.NextFrameAsync().AsTask();
        }, priority);
        Dispatcher.UIThread.RunJobs();
        _ = root.NextFrameAsync().AsTask().ContinueWith(_ => log.Add("after"), TaskContinuationOptions.ExecuteSynchronously);
        child.Dispose();
        Assert.True(canceled!.IsCanceled);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Dispatcher.UIThread.RunJobs(FrameClock.ResumePriority);
        Assert.True(waiting!.IsCompletedSuccessfully);
        Assert.Equal(["resumed", "after"], log);
        window.Close();

        async Task Wait()
        {
            await root.NextFrameAsync();
            log.Add("resumed");
        }
    });

    [Fact]
    public Task DurationScale_AttachedInherits_InstanceSettersMultiply_AndBothRaisePlaybackChanged() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var control = new Border();
        var host = new Decorator { Child = control };
        var window = Show(host, clock);
        var root = window.AnimationRoot;
        var scope = AnimationScope.Get(control);
        AnimationScope.SetDurationScale(host, 2);
        Assert.Equal(2, AnimationScope.GetDurationScale(control));
        var lifetimeClock = (IAnimationLifetimeClock)scope.Clock;
        Assert.Equal(root.DurationScale * 2, lifetimeClock.DurationScale, 7);
        root.DurationScale = 3;
        Assert.Equal(6 , lifetimeClock.DurationScale, 7);
        var child = scope.CreateChild();
        child.DurationScale = 0.5;
        Assert.Equal(0.5 * 2, ((IAnimationLifetimeClock)child.Clock).DurationScale, 7);

        var value = scope.CreateAnimatable(0d);
        var task = value.AnimateToAsync(1, Tween());
        clock.AdvanceBy(TimeSpan.Zero); Dispatcher.UIThread.RunJobs();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(300)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(.5, value.Value, 7);        // 300ms at 3x2
        AnimationScope.SetDurationScale(host, 1); // attached change: PlaybackChanged keeps progress
        clock.AdvanceBy(TimeSpan.FromMilliseconds(150)); Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, value.Value, 7);
        Assert.True(task.IsCompletedSuccessfully);
        window.Close();
    });

    [Fact]
    public Task Diagnostics_ExposeCountersStaleAndRebuildState() => OnUi(async () =>
    {
        var control = new Border();
        var panel = new StackPanel { Children = { control } };
        var window = Show(panel);
        var scope = AnimationScope.Get(control);
        var d = scope.Diagnostics;
        Assert.Same(window.AnimationRoot, d.Parent);
        Assert.False(d.IsStale); Assert.False(d.IsEnding); Assert.Equal(0, d.Generation);
        var child = scope.CreateChild();
        child.RegisterResource(() => { });
        Assert.Equal(1, scope.Diagnostics.ActiveChildCount);
        Assert.Equal(1, child.Diagnostics.SubscriptionCount);
        var error = new InvalidOperationException("boom");
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Launch(_ => throw error));
        Assert.Same(error, scope.Diagnostics.LastError);
        panel.Children.Remove(control);
        Assert.True(scope.Diagnostics.IsStale);
        Assert.Equal(1, scope.Diagnostics.WindowStaleCount);
        Dispatcher.UIThread.RunJobs();
        await scope.Diagnostics.Ended;
        window.Close();
    });

    [Fact]
    public Task ThrowingScopeChangedHandler_IsReported_AndOtherRebuildsContinue() => OnUi(() =>
    {
        var a = new Border(); var b = new Border();
        var left = new Decorator { Child = new StackPanel { Children = { a, b } } };
        var right = new Decorator { Child = new StackPanel() };
        var window = Show(new StackPanel { Children = { left, right } });
        AnimationScope.Get(left); AnimationScope.Get(right);
        AnimationScope.Get(a); AnimationScope.Get(b);
        var failures = new List<AnimationFailureEventArgs>();
        window.AnimationRoot.AnimationFailed += (_, e) => failures.Add(e);
        var error = new InvalidOperationException("handler");
        AnimationScope.AddScopeChangedHandler(a, (_, _) => throw error);
        var bEvents = Record(b);
        var source = (StackPanel)left.Child!; var target = (StackPanel)right.Child!;
        source.Children.Clear(); target.Children.Add(a); target.Children.Add(b);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(error, Assert.Single(failures).Exception);
        Assert.Same(AnimationScope.Get(a), failures[0].SourceScope);
        Assert.Single(bEvents);
        window.Close();
    });
}
