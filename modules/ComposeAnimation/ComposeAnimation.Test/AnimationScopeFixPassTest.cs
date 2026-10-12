using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public sealed class AnimationScopeFixPassTest
{
    private static Task OnUi(Action action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None);

    private static AnimationTestWindow Show(Control content, ManualFrameSource? clock = null)
    {
        var window = new AnimationTestWindow(clock ?? new ManualFrameSource()) { Content = content, Width = 300, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static List<AnimationFailureEventArgs> Failures(AnimationTestWindow window)
    {
        var failures = new List<AnimationFailureEventArgs>();
        window.AnimationRoot.AnimationFailed += (_, e) => failures.Add(e);
        return failures;
    }

    private sealed class VisualHolder : Control
    {
        public void Add(Visual visual) { LogicalChildren.Add(visual); VisualChildren.Add(visual); }
    }

    private sealed class ComponentHost(List<string> log) : Border, IAnimationScopeHost
    {
        void IAnimationScopeHost.OnScopeChanged(AnimationScopeChangedEventArgs e) => log.Add("component");
    }

    private sealed class DeferringHost : Border, IAnimationScopeHost
    {
        public bool Defer { get; set; }
        bool IAnimationScopeHost.DefersScopeRebuild => Defer;
        void IAnimationScopeHost.OnScopeChanged(AnimationScopeChangedEventArgs e) { }
    }

    // Item 1
    [Fact]
    public Task ThrowDuringRebuild_IsReported_AndTheRestOfTheDrainStillRebuilds() => OnUi(() =>
    {
        var failing = new Border();
        var healthy = new Border();
        var left = new StackPanel { Children = { failing, healthy } };
        var right = new StackPanel();
        var window = Show(new StackPanel { Children = { new Decorator { Child = left }, new Decorator { Child = right } } });
        AnimationScope.Get((Decorator)left.Parent!); AnimationScope.Get((Decorator)right.Parent!);
        var failingScope = AnimationScope.Get(failing);
        var healthyScope = AnimationScope.Get(healthy);
        var failures = Failures(window);
        var error = new InvalidOperationException("observer");
        failing.PropertyChanged += (_, e) => { if (e.Property == AnimationScope.ScopeProperty) throw error; };
        var healthyChanges = new List<AnimationScopeChangedEventArgs>();
        AnimationScope.AddScopeChangedHandler(healthy, (_, e) => healthyChanges.Add(e));
        left.Children.Clear();
        right.Children.Add(failing); right.Children.Add(healthy);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(failures, f => ReferenceEquals(f.Exception, error) && f.OperationLabel == "Scope rebuild");
        Assert.Single(healthyChanges);
        Assert.Same(healthyScope, healthyChanges[0].OldScope);
        Assert.Same(AnimationScope.Get(right.Parent as Decorator ?? throw new()), healthyChanges[0].NewScope!.Diagnostics.Parent);
        Assert.False(failingScope.Diagnostics.IsStale);
        Assert.Equal(0, window.AnimationRoot.Diagnostics.WindowStaleCount);
        Assert.False(AnimationScope.Get(failing).Diagnostics.IsEnding);
        window.Close();
    });

    // Item 2
    [Fact]
    public Task Get_OnAScopeEndedByItsParent_RebuildsItSynchronously() => OnUi(async () =>
    {
        var control = new Border();
        var parent = new Decorator { Child = control };
        var window = Show(new StackPanel { Children = { parent } });
        var parentScope = AnimationScope.Get(parent);
        var old = AnimationScope.Get(control);
        var changes = new List<AnimationScopeChangedEventArgs>();
        AnimationScope.AddScopeChangedHandler(control, (_, e) => changes.Add(e));
        var ended = parentScope.Diagnostics.EndAsync(); // cascade: the child is ending and stale
        Assert.True(old.Diagnostics.IsEnding);
        var current = AnimationScope.Get(control);
        Assert.NotSame(old, current);
        Assert.False(current.Diagnostics.IsEnding);
        var newParent = current.Diagnostics.Parent!;
        Assert.NotSame(parentScope, newParent);
        Assert.Same(AnimationScope.Get(parent), newParent);
        Assert.Single(changes);
        Assert.Same(old, changes[0].OldScope);
        Assert.Same(current, changes[0].NewScope);
        Dispatcher.UIThread.RunJobs(); // the queued drain finds nothing left to do
        Assert.Single(changes);
        Assert.Same(current, AnimationScope.Get(control));
        await ended;
        window.Close();
    });

    [Fact]
    public Task Get_FromAHandlerDuringTheDrain_NeverReturnsAnEndedScope() => OnUi(() =>
    {
        var control = new Border();
        var parent = new Decorator { Child = control };
        var left = new Decorator { Child = parent };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } });
        AnimationScope.Get(left); var rightScope = AnimationScope.Get(right);
        AnimationScope.Get(parent);
        var old = AnimationScope.Get(control);
        AnimationScope? seen = null;
        AnimationScope.AddScopeChangedHandler(parent, (_, _) => seen = AnimationScope.Get(control));
        var childChanges = 0;
        AnimationScope.AddScopeChangedHandler(control, (_, _) => childChanges++);
        left.Child = null; right.Child = parent;
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(seen);
        Assert.NotSame(old, seen);
        Assert.False(seen!.Diagnostics.IsEnding);
        Assert.Same(AnimationScope.Get(parent), seen.Diagnostics.Parent);
        Assert.Same(rightScope, AnimationScope.Get(parent).Diagnostics.Parent);
        Assert.Equal(1, childChanges);
        Assert.Same(seen, AnimationScope.Get(control));
        window.Close();
    });

    // Item 3
    [Fact]
    public Task ThrowingHandler_DoesNotStopTheNextOne_OnAnInteractiveHost_AndTheComponentRunsFirst() => OnUi(() =>
    {
        var log = new List<string>();
        var host = new ComponentHost(log);
        var left = new Decorator { Child = host };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } });
        AnimationScope.Get(left); AnimationScope.Get(right);
        AnimationScope.Get(host);
        var failures = Failures(window);
        var error = new InvalidOperationException("first");
        AnimationScope.AddScopeChangedHandler(host, (_, _) => { log.Add("first"); throw error; });
        AnimationScope.AddScopeChangedHandler(host, (_, e) => log.Add(e.Source == host ? "second" : "second-bad-source"));
        left.Child = null; right.Child = host;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["component", "first", "second"], log);
        var failure = Assert.Single(failures, f => ReferenceEquals(f.Exception, error));
        Assert.Equal("ScopeChanged handler", failure.OperationLabel);
        Assert.Same(AnimationScope.Get(host), failure.SourceScope);
        window.Close();
    });

    [Fact]
    public Task ThrowingHandler_DoesNotStopTheNextOne_OnAPlainVisual_AndSourceIsNull() => OnUi(() =>
    {
        var visual = new Visual();
        var holder = new VisualHolder();
        holder.Add(visual);
        var left = new Decorator { Child = holder };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } });
        AnimationScope.Get(left); AnimationScope.Get(right);
        var old = AnimationScope.Get(visual);
        var failures = Failures(window);
        var log = new List<string>();
        AnimationScope.AddScopeChangedHandler(visual, (_, _) => { log.Add("first"); throw new InvalidOperationException("first"); });
        AnimationScope.AddScopeChangedHandler(visual, (_, e) => log.Add(e.Source is null ? "second" : "second-bad-source"));
        left.Child = null; right.Child = holder;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["first", "second"], log);
        Assert.Single(failures, f => f.OperationLabel == "ScopeChanged handler");
        Assert.NotSame(old, AnimationScope.Get(visual));
        window.Close();
    });

    // Item 5 / S3 at the scope level
    [Fact]
    public Task DeferringHost_IsParkedWithoutPolling_AndRebuildsWhenItsPolicyClears() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var host = new DeferringHost();
        var left = new Decorator { Child = host };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } }, clock);
        var leftScope = AnimationScope.Get(left); var rightScope = AnimationScope.Get(right);
        var old = AnimationScope.Get(host);
        var changes = new List<AnimationScopeChangedEventArgs>();
        AnimationScope.AddScopeChangedHandler(host, (_, e) => changes.Add(e));
        host.Defer = true;
        left.Child = null; right.Child = host;
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(changes);
        Assert.True(old.Diagnostics.IsParked);
        Assert.Equal(0, window.AnimationRoot.Diagnostics.WindowStaleCount); // not re-queued every turn
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Same(old, AnimationScope.Get(host));
        Assert.Same(leftScope, old.Diagnostics.Parent);
        host.Defer = false;
        AnimationScope.RebuildPolicyChanged(host);
        Dispatcher.UIThread.RunJobs();
        var change = Assert.Single(changes);
        Assert.Same(old, change.OldScope);
        Assert.Same(rightScope, change.NewScope!.Diagnostics.Parent);
        window.Close();
    });

    [Fact]
    public Task ParkedHost_WhoseAncestorEnds_IsQueuedAgain() => OnUi(() =>
    {
        var host = new DeferringHost { Defer = true };
        var parent = new Decorator { Child = host };
        var left = new Decorator { Child = parent };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } });
        AnimationScope.Get(left); AnimationScope.Get(right); AnimationScope.Get(parent);
        var old = AnimationScope.Get(host);
        // Park it: a logical-tree change of the host alone.
        parent.Child = null; parent.Child = host;
        Dispatcher.UIThread.RunJobs();
        Assert.True(old.Diagnostics.IsParked);
        left.Child = null; right.Child = parent; // the parent rebuilds and ends the parked scope
        Dispatcher.UIThread.RunJobs();
        Assert.True(old.Diagnostics.IsEnding);
        var current = AnimationScope.Get(host);
        Assert.False(current.Diagnostics.IsEnding);
        Assert.Same(AnimationScope.Get(parent), current.Diagnostics.Parent);
        window.Close();
    });

    // Item 6
    [Fact]
    public Task InheritanceParent_IsTheSourceOfInheritedValues_ForContentPresenterContent() => OnUi(() =>
    {
        var content = new Border();
        var control = new ContentControl { Content = content };
        var window = Show(new StackPanel { Children = { control } });
        var above = AvaloniaInternals.GetInheritanceParent(content);
        Assert.IsType<ContentPresenter>(above);
        AssertInheritsFrom(content, above!);
        window.Close();
    });

    [Fact]
    public Task InheritanceParent_IsTheSourceOfInheritedValues_ForPopupContent() => OnUi(() =>
    {
        var anchor = new Border { Width = 40, Height = 40 };
        var panel = new StackPanel { Children = { anchor } };
        var window = Show(panel);
        var content = new Border();
        var popup = new Popup { PlacementTarget = anchor, Child = content };
        panel.Children.Add(popup);
        popup.IsOpen = true; Dispatcher.UIThread.RunJobs();
        var above = AvaloniaInternals.GetInheritanceParent(content);
        Assert.NotNull(above);
        AssertInheritsFrom(content, above!);
        popup.IsOpen = false; window.Close();
    });

    // The value the element inherits changes with the candidate's value, and with nothing above it.
    private static void AssertInheritsFrom(AvaloniaObject element, AvaloniaObject candidate)
    {
        candidate.SetValue(AnimationScope.DurationScaleProperty, 3.0);
        Assert.Equal(3.0, element.GetValue(AnimationScope.DurationScaleProperty));
        if (AvaloniaInternals.GetInheritanceParent(candidate) is { } further)
        {
            further.SetValue(AnimationScope.DurationScaleProperty, 7.0);
            Assert.Equal(3.0, element.GetValue(AnimationScope.DurationScaleProperty));
            further.ClearValue(AnimationScope.DurationScaleProperty);
        }
        candidate.ClearValue(AnimationScope.DurationScaleProperty);
        Assert.Equal(1.0, element.GetValue(AnimationScope.DurationScaleProperty));
    }

    // Item 8
    [Fact]
    public Task ScopeChangedHandlers_RequireTheUiThread() => OnUi(() =>
    {
        var control = new Border();
        EventHandler<AnimationScopeChangedEventArgs> handler = (_, _) => { };
        Exception? add = null, remove = null;
        var thread = new Thread(() =>
        {
            add = Record(() => AnimationScope.AddScopeChangedHandler(control, handler));
            remove = Record(() => AnimationScope.RemoveScopeChangedHandler(control, handler));
        });
        thread.Start(); thread.Join();
        Assert.IsType<InvalidOperationException>(add);
        Assert.IsType<InvalidOperationException>(remove);
        static Exception? Record(Action action) { try { action(); return null; } catch (Exception e) { return e; } }
    });

    [Fact]
    public Task RemoveScopeChangedHandler_RemovesOnlyThatHandler() => OnUi(() =>
    {
        var control = new Border();
        var left = new Decorator { Child = control };
        var right = new Decorator();
        var window = Show(new StackPanel { Children = { left, right } });
        AnimationScope.Get(left); AnimationScope.Get(right); AnimationScope.Get(control);
        var log = new List<string>();
        EventHandler<AnimationScopeChangedEventArgs> a = (_, _) => log.Add("a");
        AnimationScope.AddScopeChangedHandler(control, a);
        AnimationScope.AddScopeChangedHandler(control, (_, _) => log.Add("b"));
        AnimationScope.RemoveScopeChangedHandler(control, a);
        left.Child = null; right.Child = control;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["b"], log);
        window.Close();
    });
}
