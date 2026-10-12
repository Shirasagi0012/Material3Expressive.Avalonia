using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public partial class AnimatedBoundsTest
{
    private static (LookaheadBox Scene, LookaheadBox Left, LookaheadBox Right) MoveSlots(Control content)
    {
        var left = new LookaheadBox
        {
            Width = 180, Height = 200, HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, UseLayoutRounding = false, Children = { content }
        };
        var right = new LookaheadBox
        {
            Width = 180, Height = 200, HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom, UseLayoutRounding = false
        };
        return (new LookaheadBox { Children = { left, right }, UseLayoutRounding = false }, left, right);
    }

    [Fact]
    public Task MoveContent_IdleInstanceAnimatesFromOldParentAndPublishesOnlyFinalTree() => OnUi(() =>
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        using var f = new Fixture(scene);
        var before = f.Actual(card);
        var snapshot = f.Root.Snapshot!;
        var targets = 0;
        f.Root.TargetLayoutUpdated += (_, _) => targets++;
        f.Root.MoveContent(card, () => { left.Children.Remove(card); right.Children.Add(card); });
        Assert.Equal(0, targets);
        f.Layout();
        Assert.Equal(1, targets);
        Assert.NotSame(snapshot, f.Root.Snapshot);
        Equal(before, f.Actual(card));
        Assert.True(card.IsRunning);
        f.Step(16); f.Step(100);
        Equal(new Rect(110, 50, 120, 160), f.Actual(card));
        f.Step(100);
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        Assert.Equal(1, targets);
    });

    [Fact]
    public Task MoveContent_UnderADifferentAncestorScope_KeepsTheFlightAndRebuildsAfterItSettles() => OnUi(() =>
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        using var f = new Fixture(scene);
        var leftScope = AnimationScope.Get(left);
        var rightScope = AnimationScope.Get(right);
        var old = AnimationScope.Get(card);
        Assert.NotSame(rightScope, old.Diagnostics.Parent); // created during the first layout, under the root
        var changes = new List<AnimationScopeChangedEventArgs>();
        AnimationScope.AddScopeChangedHandler(card, (_, e) => changes.Add(e));
        var before = f.Actual(card);
        f.Root.MoveContent(card, () => { left.Children.Remove(card); right.Children.Add(card); });
        f.Layout();
        Assert.Empty(changes);
        Assert.True(old.Diagnostics.IsParked);
        Equal(before, f.Actual(card));
        Assert.True(card.IsRunning);
        f.Step(16); f.Step(100);
        // The rebuild waits: the retained flight continues on the pre-move scope.
        Assert.True(card.IsRunning);
        Assert.Empty(changes);
        Assert.Same(old, AnimationScope.Get(card));
        Assert.True(old.Diagnostics.IsStale);
        Equal(new Rect(110, 50, 120, 160), f.Actual(card));
        f.Step(100);
        Assert.False(card.IsRunning);
        f.Layout();
        var change = Assert.Single(changes);
        Assert.Same(old, change.OldScope);
        Assert.Same(rightScope, change.NewScope!.Diagnostics.Parent);
        Assert.Same(change.NewScope, AnimationScope.Get(card));
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task MoveContent_ActiveSpringPreservesAllFourEdgeVelocities() => OnUi(() =>
    {
        var text = Text();
        var card = Bounds(text);
        card.BoundsTransform = new BoundsTransform
        {
            Spec = new SpringSpec<Rect>(stiffness: 50, visibilityThreshold: VisibilityThresholds.Rect)
        };
        var (scene, left, right) = MoveSlots(card);
        using var f = new Fixture(scene);
        var start = f.Actual(card);
        card.TargetSize = new Size(160, 100); card.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout();
        var firstTarget = f.Root.Snapshot!.Results[card].Bounds;
        f.Step(16); f.Step(100);
        var first = new TargetBasedAnimation<Rect>(card.BoundsTransform.Spec, new RectConverter(), start, firstTarget);
        var current = first.GetValue(TimeSpan.FromMilliseconds(100));
        Equal(current, f.Actual(card));
        f.Root.MoveContent(card, () =>
        {
            left.Children.Remove(card); right.Children.Add(card);
            card.TargetSize = new Size(140, 120); card.HorizontalAlignment = HorizontalAlignment.Left;
        });
        f.Layout();
        Equal(current, f.Actual(card));
        var nextTarget = f.Root.Snapshot!.Results[card].Bounds;
        var resumed = new TargetBasedAnimation<Rect>(card.BoundsTransform.Spec, new RectConverter(), current, nextTarget,
            initialVelocity: first.GetVelocity(TimeSpan.FromMilliseconds(100)));
        f.Step(1);
        Equal(resumed.GetValue(TimeSpan.FromMilliseconds(1)), f.Actual(card));
        Equal(f.Actual(card), f.Actual(text));
        f.Step(10000);
        Equal(nextTarget, f.Actual(card));
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task MoveContent_BetweenDifferentScrollFramesDoesNotJumpAndCanMoveAgain(bool includeMotion) => OnUi(() =>
    {
        var card = Bounds(Text()); card.Margin = new Thickness(0, 150, 0, 0);
        card.AnimateMotionFrameOfReference = includeMotion;
        var leftContent = new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false };
        var rightContent = new LookaheadBox { Height = 600, UseLayoutRounding = false };
        var left = Scroll(leftContent); left.Width = 180; left.Offset = new Vector(0, 40);
        var right = Scroll(rightContent); right.Width = 180; right.Offset = new Vector(0, 140);
        using var f = new Fixture(new LookaheadStackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 40, Children = { left, right }, UseLayoutRounding = false
        });
        var before = f.Actual(card);
        Equal(new Rect(0, 110, 120, 160), before);
        f.Root.MoveContent(card, () => { leftContent.Children.Remove(card); rightContent.Children.Add(card); });
        f.Layout();
        Equal(before, f.Actual(card));
        f.Step(16); f.Step(100);
        Equal(new Rect(110, 60, 120, 160), f.Actual(card));
        var middle = f.Actual(card);
        f.Root.MoveContent(card, () => { rightContent.Children.Remove(card); leftContent.Children.Add(card); });
        f.Layout();
        Equal(middle, f.Actual(card));
        f.Step(100);
        Equal(new Rect(55, 85, 120, 160), f.Actual(card));
        f.Step(100);
        Equal(before, f.Actual(card));
        Assert.False(card.IsRunning);
        // The accumulated basis survives a second move on the fixed root clock.
        f.Root.MoveContent(card, () => { leftContent.Children.Remove(card); rightContent.Children.Add(card); });
        f.Layout(); f.Step(16); f.Step(50);
        middle = f.Actual(card);
        var clock = f.Clock;
        f.Layout();
        Equal(middle, f.Actual(card));
        clock.Pump(TimeSpan.FromMilliseconds(16)); f.Layout();
        clock.Pump(TimeSpan.FromMilliseconds(200)); f.Layout();
        Equal(new Rect(220, 10, 120, 160), f.Actual(card));
    });

    [Fact]
    public Task MoveContent_UnchangedScrollEdgeStillFollowsInputChangesImmediately() => OnUi(() =>
    {
        var card = Bounds(Text()); card.Margin = new Thickness(0, 150, 0, 0);
        var (scene, left, right) = MoveSlots(card);
        scene.Height = 600;
        right.VerticalAlignment = VerticalAlignment.Top;
        var scroll = Scroll(scene); scroll.Width = 400; scroll.Offset = new Vector(0, 40);
        using var f = new Fixture(scroll);
        var before = f.Actual(card);
        f.Root.MoveContent(card, () =>
        {
            left.Children.Remove(card); right.Children.Add(card);
            scroll.Offset = new Vector(0, 60);
        });
        f.Layout();
        Equal(new Rect(before.TopLeft - new Vector(0, 20), before.Size), f.Actual(card));
        f.Step(16); f.Step(200);
        Assert.Equal(220, f.Actual(card).X);
        Assert.Equal(90, f.Actual(card).Y);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task MoveContent_AbsoluteKeyframesUseCallbackBasisAcrossScrollChangesAndReversal(bool includeMotion) => OnUi(() =>
    {
        var calls = new List<(Rect From, Rect To)>();
        var card = Bounds(Text()); card.Margin = new Thickness(0, 150, 0, 0);
        card.AnimateMotionFrameOfReference = includeMotion;
        card.BoundsTransform = new CallbackTransform((from, to) =>
        {
            calls.Add((from, to));
            var config = new KeyframesSpecConfig<Rect> { Duration = TimeSpan.FromMilliseconds(200) };
            // An absolute (not relative) waypoint derived from the callback's coordinate basis.
            config.At(100, new Rect((from.X + to.X) / 2, (from.Y + to.Y) / 2 - 20,
                (from.Width + to.Width) / 2, (from.Height + to.Height) / 2));
            return new KeyframesSpec<Rect>(config);
        });
        var leftContent = new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false };
        var rightContent = new LookaheadBox { Height = 600, UseLayoutRounding = false };
        var left = Scroll(leftContent); left.Width = 180; left.Offset = new Vector(0, 40);
        var right = Scroll(rightContent); right.Width = 180; right.Offset = new Vector(0, 140);
        using var f = new Fixture(new LookaheadStackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 40, Children = { left, right }, UseLayoutRounding = false
        });
        Assert.Empty(calls);
        f.Root.MoveContent(card, () => { leftContent.Children.Remove(card); rightContent.Children.Add(card); });
        f.Layout();
        Assert.Equal(2, calls.Count);
        Assert.All(calls, call =>
        {
            Equal(new Rect(0, includeMotion ? 110 : 150, 120, 160), call.From);
            Equal(new Rect(220, includeMotion ? 10 : 50, 120, 160), call.To);
        });
        f.Step(16); f.Step(100);
        Equal(new Rect(110, 40, 120, 160), f.Actual(card));
        calls.Clear();
        f.Root.MoveContent(card, () => { rightContent.Children.Remove(card); leftContent.Children.Add(card); });
        f.Layout();
        Equal(new Rect(110, 40, 120, 160), f.Actual(card));
        Assert.Equal(2, calls.Count);
        Assert.All(calls, call =>
        {
            Equal(new Rect(110, includeMotion ? 40 : 80, 120, 160), call.From);
            Equal(new Rect(0, includeMotion ? 110 : 150, 120, 160), call.To);
        });
        f.Step(100);
        Equal(new Rect(55, 55, 120, 160), f.Actual(card));
        f.Step(100);
        Equal(new Rect(0, 110, 120, 160), f.Actual(card));
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task MoveContent_CapturesNestedBoundsAndKeepsTheirIndependentTimelines() => OnUi(() =>
    {
        var child = Bounds(Text()); child.TargetSize = new Size(60, 70); child.BoundsTransform = Tween(400);
        var innerScope = new LookaheadBox { Children = { child }, UseLayoutRounding = false };
        child.ReferenceScope = innerScope;
        var parent = Bounds(innerScope); parent.TargetSize = new Size(180, 200);
        var (scene, left, right) = MoveSlots(parent);
        using var f = new Fixture(scene);
        child.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout(); f.Step(16); f.Step(100);
        Assert.Equal(30, LookaheadCoordinates.ActualPosition(child, innerScope).X, 5);
        f.Root.MoveContent(parent, () => { left.Children.Remove(parent); right.Children.Add(parent); });
        f.Layout();
        Assert.Equal(30, LookaheadCoordinates.ActualPosition(child, innerScope).X, 5);
        f.Step(0); // Establish the newly started parent's first frame without advancing the child.
        f.Step(100);
        Assert.Equal(60, LookaheadCoordinates.ActualPosition(child, innerScope).X, 5);
        f.Step(100);
        Assert.False(parent.IsRunning);
        Assert.True(child.IsRunning);
        Assert.Equal(90, LookaheadCoordinates.ActualPosition(child, innerScope).X, 5);
        f.Step(100);
        Assert.False(child.IsRunning);
        Equal(f.Root.Snapshot!.Results[child].Bounds, f.Actual(child));
    });

    [Fact]
    public Task MoveContent_CallbackFailureReleasesStateWithoutRollingBackTreeEdits() => OnUi(() =>
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        using var f = new Fixture(scene);
        card.TargetSize = new Size(160, 100); f.Layout(); f.Step(16); f.Step(50);
        var error = new InvalidOperationException("tree update failed");
        Assert.Same(error, Assert.Throws<InvalidOperationException>(() => f.Root.MoveContent(card, () =>
        {
            left.Children.Remove(card);
            throw error;
        })));
        Assert.DoesNotContain(card, left.Children);
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        right.Children.Add(card); f.Layout();
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
        Assert.False(card.IsRunning);
    });

    [Fact]
    public Task MoveContent_CrossRootMoveReleasesMotionAndReportsUnsupportedRetention() => OnUi(() =>
    {
        var card = Bounds(Text());
        var source = new LookaheadBox { Children = { card } };
        var destination = new LookaheadBox();
        using var first = new Fixture(source);
        using var second = new Fixture(destination);
        card.TargetSize = new Size(240, 100); first.Layout(); first.Step(16); first.Step(50);
        Assert.Throws<InvalidOperationException>(() => first.Root.MoveContent(card, () =>
        {
            source.Children.Remove(card); destination.Children.Add(card);
        }));
        Assert.False(card.IsRunning);
        Assert.Equal(0, first.Clock.PendingFrameCount);
        first.Layout(); second.Layout();
        Equal(second.Root.Snapshot!.Results[card].Bounds, second.Actual(card));
    });

    [Fact]
    public Task MoveContent_LeavingExplicitReferenceCancelsUntilReferenceIsCorrected() => OnUi(() =>
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        card.ReferenceScope = left;
        using var f = new Fixture(scene);
        Assert.Throws<InvalidOperationException>(() => f.Root.MoveContent(card, () =>
        {
            left.Children.Remove(card); right.Children.Add(card);
        }));
        Assert.False(card.IsRunning);
        card.ReferenceScope = null; f.Layout();
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task MoveContent_ExplicitResetOrHiddenDestinationDoesNotRetainMotion(bool hide) => OnUi(() =>
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        using var f = new Fixture(scene);
        card.TargetSize = new Size(160, 100); f.Layout(); f.Step(16); f.Step(50);
        f.Root.MoveContent(card, () =>
        {
            left.Children.Remove(card); right.Children.Add(card);
            if (hide) right.IsVisible = false;
            else card.ResetAnimation();
        });
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        f.Layout(); right.IsVisible = true; f.Layout();
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
    });

    [Fact]
    public Task MoveContent_RejectsReentrantLayoutAndNestedEditsAndRecovers() => OnUi(() =>
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        using var f = new Fixture(scene);
        card.TargetSize = new Size(160, 100); f.Layout();
        Assert.Throws<InvalidOperationException>(() => f.Root.MoveContent(card, () =>
        {
            left.Children.Remove(card); right.Children.Add(card);
            f.Window.UpdateLayout();
        }));
        Assert.False(card.IsRunning);
        f.Layout();
        Assert.Throws<InvalidOperationException>(() => f.Root.MoveContent(card, () => f.Root.MoveContent(card, () => { })));
        f.Layout();
        f.Root.MoveContent(card, () => { right.Children.Remove(card); left.Children.Add(card); });
        f.Layout(); f.Step(16); f.Step(200);
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
    });

    [Fact]
    public Task MoveContent_RejectsAsyncVoidCallbackBeforeItRuns() => OnUi(() =>
    {
        var card = Bounds(Text());
        using var f = new Fixture(new LookaheadBox { Children = { card } });
        var called = false;
        Action asyncEdit = async () => { called = true; await Task.Yield(); };
        Assert.Throws<ArgumentException>(() => f.Root.MoveContent(card, asyncEdit));
        Assert.False(called);
    });

    [Fact]
    public Task MoveContent_RejectsAnimationFramesDuringTreeEditAndReleasesAllNestedRuns() => OnUi(() =>
    {
        var child = Bounds(Text()); child.TargetSize = new Size(60, 70);
        var parent = Bounds(new LookaheadBox { Children = { child } });
        var (scene, left, right) = MoveSlots(parent);
        using var f = new Fixture(scene);
        parent.TargetSize = new Size(160, 100); child.TargetSize = new Size(90, 80);
        f.Layout(); f.Step(16);
        Assert.Throws<InvalidOperationException>(() => f.Root.MoveContent(parent, () =>
        {
            left.Children.Remove(parent); right.Children.Add(parent);
            f.Step(20); // Pump frame continuations and layout while the tree edit is still open.
        }));
        Assert.False(parent.IsRunning);
        Assert.False(child.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        f.Layout();
        Equal(f.Root.Snapshot!.Results[parent].Bounds, f.Actual(parent));
    });


    [Fact]
    public Task MoveContent_OrdinaryRemovalAfterCommitStillReleasesImmediately() => OnUi(() =>
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        using var f = new Fixture(scene);
        f.Root.MoveContent(card, () => { left.Children.Remove(card); right.Children.Add(card); });
        f.Layout(); f.Step(16); f.Step(50);
        Assert.True(card.IsRunning);
        right.Children.Remove(card);
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        left.Children.Add(card); f.Layout();
        Assert.False(card.IsRunning);
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
    });

    [Fact]
    public Task MoveContent_DirectIntermediateMeasureCannotBeSilentlyIgnored() => OnUi(() =>
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        using var f = new Fixture(scene);
        Assert.Throws<InvalidOperationException>(() => f.Root.MoveContent(card, () =>
        {
            left.Children.Remove(card);
            card.InvalidateMeasure();
            Assert.Throws<InvalidOperationException>(() => card.Measure(new Size(180, 200)));
            right.Children.Add(card);
        }));
        f.Layout();
        Assert.False(card.IsRunning);
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
    });
}
