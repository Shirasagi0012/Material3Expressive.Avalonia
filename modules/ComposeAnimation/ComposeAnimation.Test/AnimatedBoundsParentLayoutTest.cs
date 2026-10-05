using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ComposeAnimation.Test;

public partial class AnimatedBoundsTest
{
    [Fact]
    public Task TargetSizeMode_ReservesDestinationWhilePresentationAnimates() => OnUi(() =>
    {
        var text = Text();
        var card = Bounds(text);
        var sibling = new LookaheadBoundary { Child = new Border { Width = 10, Height = 10 } };
        var stack = new LookaheadStackPanel { Children = { card, sibling }, UseLayoutRounding = false };
        using var f = new Fixture(stack, BoundsParentLayoutMode.TargetSize);
        card.TargetSize = new Size(240, 100);
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        var target = snapshot.Results[card].Bounds;
        Equal(target, f.Actual(card));
        Equal(new Rect(0, 0, 120, 160), card.CurrentBounds!.Value);
        Assert.Equal(100, sibling.Bounds.Y);
        f.Step(16); f.Step(100);
        Equal(new Rect(0, 0, 180, 130), card.CurrentBounds!.Value);
        Equal(card.CurrentBounds.Value, f.Actual(text));
        Equal(target, f.Actual(card));
        Assert.Equal(100, sibling.Bounds.Y);
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Step(100);
        Equal(card.TargetBounds!.Value, card.CurrentBounds!.Value);
        Assert.False(card.IsRunning);
    });

    [Fact]
    public Task SwitchingParentLayoutMode_PreservesSampleAndTargetGeneration() => OnUi(() =>
    {
        var text = Text();
        var card = Bounds(text);
        var sibling = new LookaheadBoundary { Child = new Border { Width = 10, Height = 10 } };
        using var f = new Fixture(new LookaheadStackPanel { Children = { card, sibling }, UseLayoutRounding = false },
            BoundsParentLayoutMode.TargetSize);
        card.TargetSize = new Size(240, 100);
        f.Layout(); f.Step(16); f.Step(100);
        var snapshot = f.Root.Snapshot!;
        var sample = card.CurrentBounds!.Value;
        card.ParentLayoutMode = BoundsParentLayoutMode.AnimatedSize;
        f.Layout();
        Equal(sample, card.CurrentBounds!.Value);
        Equal(sample, f.Actual(text));
        Assert.Equal(sample.Height, sibling.Bounds.Y);
        Assert.Same(snapshot, f.Root.Snapshot);
        card.ParentLayoutMode = BoundsParentLayoutMode.TargetSize;
        f.Layout();
        Equal(sample, f.Actual(text));
        Assert.Equal(100, sibling.Bounds.Y);
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Step(100);
        Assert.False(card.IsRunning);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task TargetSizeMode_MotionFramesUpdatePublishedAndRealGeometry(bool animateMotion) => OnUi(() =>
    {
        var text = Text();
        var card = Bounds(text);
        card.AnimateMotionFrameOfReference = animateMotion;
        var scroll = Scroll(new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false });
        using var f = new Fixture(scroll, BoundsParentLayoutMode.TargetSize);
        var snapshot = f.Root.Snapshot!;
        scroll.Offset = new Vector(0, 80); f.Layout();
        Equal(new Rect(0, -80, 120, 160), f.Actual(card));
        Equal(new Rect(0, -80, 120, 160), card.TargetBounds!.Value);
        Equal(new Rect(0, animateMotion ? 0 : -80, 120, 160), card.CurrentBounds!.Value);
        f.Step(16); f.Step(100);
        Equal(new Rect(0, animateMotion ? -40 : -80, 120, 160), card.CurrentBounds!.Value);
        Equal(card.CurrentBounds.Value, f.Actual(text));
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    [Fact]
    public Task NativeParent_RejectsAnimatedSizeAndRecoversInTargetSizeMode() => OnUi(() =>
    {
        var card = Bounds(new LookaheadBoundary { Child = new Border() });
        var grid = new Grid { Children = { card } };
        card.ParentLayoutMode = BoundsParentLayoutMode.AnimatedSize;
        Assert.Throws<NotSupportedException>(() => grid.Measure(new Size(300, 200)));
        card.ParentLayoutMode = BoundsParentLayoutMode.TargetSize;
        grid.InvalidateMeasure();
        grid.Measure(new Size(300, 200)); grid.Arrange(new Rect(0, 0, 300, 200));
        Assert.NotNull(card.TargetBounds);
        Assert.False(card.IsRunning);
    });

    [Fact]
    public Task ReferenceElementAlias_UsesSamePropertyAndKeepsReferenceCoordinates() => OnUi(() =>
    {
        var text = Text();
        var card = Bounds(text);
        var box = new LookaheadBox { Margin = new Thickness(20, 30, 0, 0), Children = { card } };
        card.ReferenceElement = box;
        Assert.Same(AnimatedBounds.ReferenceScopeProperty, AnimatedBounds.ReferenceElementProperty);
        Assert.Same(box, card.ReferenceScope);
        using var f = new Fixture(box, BoundsParentLayoutMode.TargetSize);
        Equal(new Rect(0, 0, 120, 160), card.CurrentBounds!.Value);
        Equal(new Rect(20, 30, 120, 160), f.Actual(text));
    });

    [Fact]
    public Task TargetBounds_IsReadableAtCommitAndNullWhenAPropertyListenerInvalidatesIt() => OnUi(() =>
    {
        var card = Bounds(new LookaheadTextBlock { Text = "local" });
        var grid = new Grid { Children = { card }, Width = 300, Height = 200 };
        var commits = 0;
        Lookahead.AddTargetLayoutUpdatedHandler(card, (_, args) =>
        {
            commits++;
            Equal(args.Snapshot.Results[card].Bounds, card.TargetBounds!.Value);
        });
        grid.Measure(new Size(300, 200)); grid.Arrange(new Rect(0, 0, 300, 200));
        Assert.Equal(1, commits);
        Rect? lastNotified = card.TargetBounds;
        card.PropertyChanged += (_, args) =>
        {
            if (args.Property == AnimatedBounds.CurrentBoundsProperty) card.TargetSize = new Size(100, 80);
            if (args.Property == AnimatedBounds.TargetBoundsProperty) lastNotified = (Rect?)args.NewValue;
        };
        card.TargetSize = new Size(200, 100);
        grid.Measure(new Size(300, 200)); grid.Arrange(new Rect(0, 0, 300, 200));
        Assert.Null(card.TargetBounds);
        Assert.Null(lastNotified);
    });
}
