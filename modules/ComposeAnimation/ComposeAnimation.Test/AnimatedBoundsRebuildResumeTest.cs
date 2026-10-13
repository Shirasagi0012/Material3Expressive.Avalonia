using Avalonia;
using Avalonia.Threading;

namespace ComposeAnimation.Test;

/// <summary>D17 for <see cref="AnimatedBounds"/>: a plain reparent (no move transaction) resumes.</summary>
public partial class AnimatedBoundsTest
{
    private static (AnimatedBounds Card, Fixture F, LookaheadBox Left, LookaheadBox Right) ReparentScene()
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        var f = new Fixture(scene);
        AnimationScope.Get(left); AnimationScope.Get(right); AnimationScope.Get(card);
        return (card, f, left, right);
    }

    [Fact]
    public Task AnimatedBounds_ReparentedMidFlightOutsideAMove_ResumesWithoutJump() => OnUi(() =>
    {
        var (card, f, left, right) = ReparentScene();
        using var _ = f;
        card.TargetSize = new Size(60, 80); f.Layout();
        f.Step(16); f.Step(48);
        var before = f.Actual(card);
        Assert.InRange(before.Width, 61, 119);
        var oldScope = AnimationScope.Get(card);
        left.Children.Remove(card); right.Children.Add(card);
        f.Step(16); // the drain rebuilds and the boundaries resume in the new scope
        Assert.NotSame(oldScope, AnimationScope.Get(card));
        var after = f.Actual(card);
        Assert.True(card.IsRunning);
        Assert.InRange(before.Width - after.Width, 0, 5); // at most one frame, no snap to 60
        Assert.InRange(Math.Abs(after.X - before.X) + Math.Abs(after.Y - before.Y), 0, 10); // no jump to the new slot
        for (var i = 0; i < 40 && card.IsRunning; i++) f.Step(16);
        Assert.False(card.IsRunning);
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
    });

    [Fact]
    public Task AnimatedBounds_DetachedWithoutReturn_IsReleasedAtTheDrain() => OnUi(() =>
    {
        var (card, f, left, _) = ReparentScene();
        using var __ = f;
        card.TargetSize = new Size(60, 80); f.Layout(); f.Step(16);
        left.Children.Remove(card);
        Assert.True(card.IsRunning); // not released on detach
        Dispatcher.UIThread.RunJobs(); // fallback post drains
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    [Fact]
    public Task AnimatedBounds_ReentrantGetInScopeChanged_LeavesOneLiveRunPerBoundary() => OnUi(() =>
    {
        var (card, f, left, right) = ReparentScene();
        using var _ = f;
        AnimationScope.AddScopeChangedHandler(card, (_, e) => { if (e.NewScope is not null) _ = AnimationScope.Get(card); });
        card.TargetSize = new Size(60, 80); f.Layout(); f.Step(16); f.Step(16);
        left.Children.Remove(card); right.Children.Add(card);
        f.Step(16);
        Assert.Equal(1, f.Frames.PendingFrameCount);
        for (var i = 0; i < 40 && card.IsRunning; i++) f.Step(16);
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
    });

    [Fact]
    public Task AnimatedBounds_ResetAnimationAfterReparent_StillResetsImmediately() => OnUi(() =>
    {
        var (card, f, _, _) = ReparentScene();
        using var __ = f;
        card.TargetSize = new Size(60, 80); f.Layout(); f.Step(16);
        card.ResetAnimation(); f.Layout();
        Assert.False(card.IsRunning);
        Equal(f.Root.Snapshot!.Results[card].Bounds, f.Actual(card));
    });

    [Fact]
    public Task AnimatedBounds_ReparentedIntoAMotionFrameOutsideAMove_ReleasesInsteadOfResumingWithAWrongBasis() => OnUi(() =>
    {
        var card = Bounds(Text());
        var (scene, left, right) = MoveSlots(card);
        var motion = new MotionHost { UseLayoutRounding = false };
        right.Children.Add(motion);
        using var f = new Fixture(scene);
        motion.Offset = new Vector(0, 30); f.Layout();
        AnimationScope.Get(left); AnimationScope.Get(motion); AnimationScope.Get(card);
        card.TargetSize = new Size(60, 80); f.Layout(); f.Step(16); f.Step(48);
        Assert.True(card.IsRunning);
        left.Children.Remove(card); motion.Child = card;
        f.Step(16);
        // No committed basis delta for the motion-frame edge: release (snap) as before (Q-H).
        Assert.False(card.IsRunning);
        Equal(new Rect(f.Root.Snapshot!.Results[card].Bounds.TopLeft + new Vector(0, 30), f.Root.Snapshot.Results[card].Bounds.Size), f.Actual(card));
    });
}
