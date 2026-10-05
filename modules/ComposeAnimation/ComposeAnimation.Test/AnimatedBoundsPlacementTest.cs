using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ComposeAnimation.Test;

public partial class AnimatedBoundsTest
{
    private sealed class PlacementLayout : BoundsLayout
    {
        public int Measures { get; private set; }
        public List<BoundsPlacementContext> Placements { get; } = [];
        public Func<BoundsPlacementContext, Point> PlaceOffset { get; set; } = _ => new Point(10, 20);
        public override BoundsLayoutResult Measure(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
        {
            Measures++;
            var child = measureChild(constraints);
            return new(constraints.Constrain(child), default)
            {
                Placement = context => { Placements.Add(context); return PlaceOffset(context); }
            };
        }
    }

    [Fact]
    public Task PlacementOnlyFrames_ReuseMeasuredPolicyAndSupplyCurrentOriginThroughCompletion() => OnUi(() =>
    {
        var text = Text();
        var card = Bounds(text);
        var policy = new PlacementLayout
        {
            // Target remains independent; changing actual intermediate placement is compensated
            // by the inner bounds boundary, rather than bending its rectangle trajectory.
            PlaceOffset = context => new Point(context.Origin.X / 2 + 10, 20)
        };
        card.IntermediateLayout = policy;
        using var f = new Fixture(new LookaheadBox { Children = { card } });
        Equal(new Rect(10, 20, 120, 160), f.Actual(text));
        card.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout(); f.Step(16);
        var snapshot = f.Root.Snapshot!;
        Equal(new Rect(430, 20, 120, 160), snapshot.Results[text].Bounds);
        var measures = policy.Measures;
        var textMeasures = text.MeasureCount;
        policy.Placements.Clear();
        f.Step(100);
        Equal(new Rect(220, 20, 120, 160), f.Actual(text));
        Assert.Contains(policy.Placements, c => !c.IsLookingAhead && c.Origin == new Point(140, 0));
        Assert.DoesNotContain(policy.Placements, c => c.IsLookingAhead);
        Assert.Equal(measures, policy.Measures);
        Assert.Equal(textMeasures, text.MeasureCount);
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Step(100);
        Equal(snapshot.Results[text].Bounds, f.Actual(text));
        Assert.Equal(measures, policy.Measures);
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task LocalEntry_PlacementCallbackRunsAgainWithoutChangingCommittedTarget() => OnUi(() =>
    {
        var policy = new PlacementLayout();
        var child = new LookaheadBoundary { Child = new Border() };
        var card = Bounds(child); card.IntermediateLayout = policy;
        card.Measure(new Size(400, 300)); card.Arrange(new Rect(0, 0, 120, 160));
        Assert.Equal(new Point(10, 20), child.Bounds.TopLeft);
        Assert.Contains(policy.Placements, context => context.IsLookingAhead);
        policy.Placements.Clear();
        var measures = policy.Measures;
        policy.PlaceOffset = _ => new Point(30, 40);
        card.InvalidateArrange(); card.Arrange(new Rect(0, 0, 120, 160));
        Assert.Equal(new Point(10, 20), child.Bounds.TopLeft);
        Assert.Equal(measures, policy.Measures);
        Assert.All(policy.Placements, context => Assert.False(context.IsLookingAhead));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task PlacementFailure_StopsFramesAndRecoversWithoutPublishingPartialTarget(bool targetFailure) => OnUi(() =>
    {
        var text = Text();
        var card = Bounds(text);
        var policy = new PlacementLayout(); card.IntermediateLayout = policy;
        using var f = new Fixture(new LookaheadBox { Children = { card } });
        card.HorizontalAlignment = HorizontalAlignment.Right; f.Layout(); f.Step(16); f.Step(50);
        Assert.True(card.IsRunning);
        var targets = 0;
        f.Root.TargetLayoutUpdated += (_, _) => targets++;
        policy.PlaceOffset = context => context.IsLookingAhead == targetFailure
            ? new Point(double.NaN, 0) : default;
        if (targetFailure) f.Root.InvalidateTargetLayout();
        else f.Root.InvalidateApproachPlacement();
        Assert.Throws<InvalidOperationException>(f.Layout);
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        Assert.Equal(0, targets);
        if (targetFailure) Assert.Null(f.Root.Snapshot);
        card.IntermediateLayout = BoundsLayout.Empty; f.Layout();
        Equal(f.Root.Snapshot!.Results[text].Bounds, f.Actual(text));
        Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(card)!).AnimationRoot.LastError);
    });

    [Fact]
    public Task PlacementReset_DoesNotContinueUsingReleasedAnimationState() => OnUi(() =>
    {
        var card = Bounds(Text());
        var policy = new PlacementLayout(); card.IntermediateLayout = policy;
        using var f = new Fixture(new LookaheadBox { Children = { card } });
        card.HorizontalAlignment = HorizontalAlignment.Right; f.Layout(); f.Step(16);
        var reset = false;
        policy.PlaceOffset = context =>
        {
            if (!context.IsLookingAhead && !reset) { reset = true; card.ResetAnimation(); }
            return new Point(10, 20);
        };
        f.Step(50);
        Assert.True(reset);
        Assert.False(card.IsRunning);
        Equal(f.Root.Snapshot!.Results[(Control)card.Child!].Bounds, f.Actual(card.Child!));
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task PlacementInvalidations_CoalesceUntilLayoutAndAcceptInvalidationDuringPlacement() => OnUi(() =>
    {
        var policy = new PlacementLayout();
        var card = Bounds(Text()); card.IntermediateLayout = policy;
        using var f = new Fixture(new LookaheadBox { Children = { card } });
        var passes = f.Root.ApproachInvalidationPassCount;
        var invalidated = false;
        var placements = 0;
        policy.PlaceOffset = context =>
        {
            if (!context.IsLookingAhead)
            {
                placements++;
                if (!invalidated) { invalidated = true; f.Root.InvalidateApproachPlacement(); }
            }
            return new Point(10, 20);
        };
        for (var i = 0; i < 100; i++) f.Root.InvalidateApproachPlacement();
        Assert.Equal(passes + 1, f.Root.ApproachInvalidationPassCount);
        f.Layout();
        Assert.Equal(passes + 2, f.Root.ApproachInvalidationPassCount);
        Assert.True(placements >= 2);
        Assert.True(f.Root.IsArrangeValid);
    });

    [Theory]
    [InlineData(100, 200)]
    [InlineData(200, 100)]
    public Task NestedPlacementCallbacks_KeepCurrentOriginsWhileScrollingAndEitherAnimationFinishes(int outerMs, int innerMs) => OnUi(() =>
    {
        var text = Text();
        var child = Bounds(text); child.TargetSize = new Size(60, 70); child.BoundsTransform = Tween(innerMs);
        child.HorizontalAlignment = HorizontalAlignment.Right; child.VerticalAlignment = VerticalAlignment.Bottom;
        var policy = new PlacementLayout(); child.IntermediateLayout = policy;
        var parent = Bounds(new LookaheadBox { Children = { child }, UseLayoutRounding = false });
        parent.TargetSize = new Size(180, 160); parent.BoundsTransform = Tween(outerMs);
        var scroll = Scroll(new LookaheadBox { Height = 600, Children = { parent }, UseLayoutRounding = false });
        using var f = new Fixture(scroll);
        var start = f.Actual(text);
        parent.TargetSize = new Size(280, 220); parent.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        var target = snapshot.Results[text].Bounds;
        f.Step(16); f.Step(150);
        policy.Placements.Clear();
        scroll.Offset = new Vector(0, 80); f.Layout();
        var fraction = Math.Min(1, 150.0 / innerMs);
        Equal(new Rect(start.X + (target.X - start.X) * fraction,
            start.Y + (target.Y - start.Y) * fraction - 80, start.Width, start.Height), f.Actual(text));
        Assert.Contains(policy.Placements, context => !context.IsLookingAhead && context.Origin == f.Actual(child).TopLeft);
        Assert.DoesNotContain(policy.Placements, context => context.IsLookingAhead);
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Step(50);
        Equal(new Rect(target.TopLeft - new Vector(0, 80), target.Size), f.Actual(text));
        Assert.False(parent.IsRunning);
        Assert.False(child.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task ManySimultaneousPositionAnimations_ShareOneInvalidationTraversalPerFrame() => OnUi(() =>
    {
        var texts = Enumerable.Range(0, 24).Select(_ => Text()).ToArray();
        var cards = texts.Select(text => Bounds(text)).ToArray();
        var box = new LookaheadBox { UseLayoutRounding = false };
        foreach (var card in cards) box.Children.Add(card);
        using var f = new Fixture(box);
        foreach (var card in cards) card.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout(); f.Step(16);
        var snapshot = f.Root.Snapshot;
        var counts = texts.Select(text => text.MeasureCount).ToArray();
        var passes = f.Root.ApproachInvalidationPassCount;
        f.Step(100);
        Assert.Equal(passes + 1, f.Root.ApproachInvalidationPassCount);
        for (var i = 0; i < cards.Length; i++)
        {
            Equal(new Rect(140, 0, 120, 160), f.Actual(texts[i]));
            Assert.Equal(counts[i], texts[i].MeasureCount);
        }
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Step(100);
        Assert.All(cards, card => Assert.False(card.IsRunning));
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });
}
